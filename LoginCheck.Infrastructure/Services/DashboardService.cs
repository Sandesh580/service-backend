using LoginCheck.Application.DTOs;
using LoginCheck.Application.Interfaces;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var total = await _context.ServiceRequests
            .CountAsync(r => r.Status != "Completed" && r.Status != "Rejected");

        var pending = await _context.ServiceRequests
            .CountAsync(r => r.Status == "PendingApproval" || r.Status == "TechnicianPending");

        var inProgress = await _context.ServiceRequests
            .CountAsync(r => r.Status == "InProgress");

        var completed = await _context.ServiceRequests
            .CountAsync(r => r.Status == "Completed");

        return new DashboardStatsDto
        {
            TotalActiveRequests = total,
            PendingApprovals = pending,
            InProgressServices = inProgress,
            CompletedJobs = completed,
            ActiveRequestsGrowth = 12.0
        };
    }

    public async Task<IEnumerable<ServiceRequestDto>> GetRequestsAsync(string? status = null, string? search = null)
    {
        var query = _context.ServiceRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.CustomerName.Contains(search) ||
                r.Appliance.Contains(search) ||
                r.RequestNumber.Contains(search) ||
                r.Issue.Contains(search));

        return await query
            .OrderByDescending(r => r.Date)
            .Select(r => new ServiceRequestDto
            {
                Id = r.Id,
                RequestNumber = r.RequestNumber,
                CustomerName = r.CustomerName,
                Appliance = r.Appliance,
                Issue = r.Issue,
                ProblemDescription = r.ProblemDescription,
                Status = r.Status,
                Priority = r.Priority,
                AmountPaid = r.AmountPaid,
                AssignedTechnician = r.AssignedTechnician,
                Date = r.Date,
                AssignedTechnicianId = r.AssignedTechnicianId,
                CustomerUserId = r.CustomerUserId,
                ApplianceBrand = r.ApplianceBrand,
                ApplianceModel = r.ApplianceModel,
                SerialNumber = r.SerialNumber,
                CustomerPhone = r.CustomerPhone,
                ServiceAddress = r.ServiceAddress,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                PreferredServiceDate = r.PreferredServiceDate,
                NextVisitDate = r.NextVisitDate,
                IssueCategory = r.IssueCategory,
                TechnicianNote = r.TechnicianNote,
                ManagerNote = r.ManagerNote,
                RejectionReason = r.RejectionReason,
                TechnicianActionAt = r.TechnicianActionAt,
                AssignedParts = r.Parts.Select(link => new ServiceRequestPartDto
                {
                    Id = link.Id, InventoryPartId = link.InventoryPartId, Sku = link.InventoryPart.Sku,
                    Name = link.InventoryPart.Name, ApplianceType = link.InventoryPart.ApplianceType,
                    Quantity = link.Quantity, AssignedAt = link.AssignedAt
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<ServiceRequestDto> CreateRequestAsync(CreateServiceRequestDto dto)
    {
        var lastRequestNumber = await _context.ServiceRequests.OrderByDescending(item => item.Id).Select(item => item.RequestNumber).FirstOrDefaultAsync();
        var nextNumber = int.TryParse(lastRequestNumber, out var previousNumber) ? previousNumber + 1 : 2401;
        var assignedName = string.IsNullOrWhiteSpace(dto.AssignedTechnician) ? null : dto.AssignedTechnician.Trim();
        var priority = string.IsNullOrWhiteSpace(dto.Priority) ? "-" : dto.Priority.Trim();
        if (priority is not ("-" or "Low" or "Medium" or "High"))
            throw new InvalidOperationException("Priority must be unset, Low, Medium, or High.");
        var assignedId = assignedName == null ? null : await _context.Users
            .Where(user => user.Role == "Technician" && user.UserName == assignedName)
            .Select(user => (int?)user.Id).FirstOrDefaultAsync();
        var request = new ServiceRequest
        {
            RequestNumber = nextNumber.ToString(), CustomerName = dto.CustomerName, Appliance = dto.Appliance,
            Issue = dto.Issue, Status = "New", Priority = priority, AssignedTechnician = assignedName,
            AssignedTechnicianId = assignedId, ManagerNote = dto.ManagerNote?.Trim() ?? string.Empty,
            Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();
        if (assignedId is not null)
        {
            AddTechnicianNotification(assignedId.Value, request, "New service call assigned",
                ManagerMessage(request, "A new service call has been assigned to you.", request.ManagerNote));
            await _context.SaveChangesAsync();
        }
        return ToResponse(request);
    }

    public async Task<ServiceRequestDto?> UpdateAssignmentAndPriorityAsync(int id, string priority, string? assignedTechnician, string? managerNote)
    {
        var normalizedPriority = priority.Trim();
        if (normalizedPriority is not ("-" or "Low" or "Medium" or "High"))
            throw new InvalidOperationException("Priority must be unset, Low, Medium, or High.");
        var request = await _context.ServiceRequests.Include(item => item.Parts).ThenInclude(link => link.InventoryPart)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return null;

        var previousTechnicianId = request.AssignedTechnicianId;
        var previousPriority = request.Priority;
        request.Priority = normalizedPriority;
        request.AssignedTechnician = string.IsNullOrWhiteSpace(assignedTechnician) ? null : assignedTechnician.Trim();
        request.AssignedTechnicianId = request.AssignedTechnician == null
            ? null
            : await _context.Users.Where(user => user.Role == "Technician" && user.UserName == request.AssignedTechnician)
                .Select(user => (int?)user.Id).FirstOrDefaultAsync();
        request.ManagerNote = managerNote?.Trim() ?? string.Empty;
        var reassigned = previousTechnicianId != request.AssignedTechnicianId;
        if (reassigned && request.Status == "Rejected" && request.AssignedTechnicianId is not null)
        {
            request.Status = "Scheduled";
            request.TechnicianNote = "Reassigned by the service manager after the previous technician declined the call.";
            request.RejectionReason = string.Empty;
        }
        if (reassigned && previousTechnicianId is not null)
            AddTechnicianNotification(previousTechnicianId.Value, request, "Service call reassigned",
                ManagerMessage(request, "This service call has been reassigned to another technician.", request.ManagerNote));
        if (request.AssignedTechnicianId is not null && (reassigned || previousPriority != request.Priority || !string.IsNullOrWhiteSpace(request.ManagerNote)))
        {
            var title = reassigned ? "Service call assigned" : "Service call updated by manager";
            var summary = reassigned ? "A service call has been assigned to you." : $"The manager updated this call. Priority: {request.Priority}.";
            AddTechnicianNotification(request.AssignedTechnicianId.Value, request, title, ManagerMessage(request, summary, request.ManagerNote));
        }
        await _context.SaveChangesAsync();
        return ToResponse(request);
    }

    public async Task<ServiceRequestDto?> UpdateStatusAsync(int id, string status, string? managerNote)
    {
        var request = await _context.ServiceRequests.Include(item => item.Parts).ThenInclude(link => link.InventoryPart)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return null;
        var previousStatus = request.Status;
        request.Status = status.Trim();
        request.ManagerNote = managerNote?.Trim() ?? string.Empty;
        if (request.AssignedTechnicianId is not null && (previousStatus != request.Status || !string.IsNullOrWhiteSpace(request.ManagerNote)))
        {
            AddTechnicianNotification(request.AssignedTechnicianId.Value, request, "Service call updated by manager",
                ManagerMessage(request, $"The manager changed the call status to {request.Status}.", request.ManagerNote));
        }
        await _context.SaveChangesAsync();
        return ToResponse(request);
    }

    private void AddTechnicianNotification(int technicianId, ServiceRequest request, string title, string message)
    {
        _context.ServiceNotifications.Add(new ServiceNotification
        {
            RecipientUserId = technicianId,
            ServiceRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            Title = title,
            Message = message.Length > 1000 ? message[..1000] : message,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string ManagerMessage(ServiceRequest request, string summary, string? remark)
    {
        var message = $"Request #{request.RequestNumber}: {summary}";
        return string.IsNullOrWhiteSpace(remark) ? message : $"{message} Manager remark: {remark.Trim()}";
    }

    private static ServiceRequestDto ToResponse(ServiceRequest request) => new()
    {
        Id = request.Id, RequestNumber = request.RequestNumber, CustomerName = request.CustomerName,
        Appliance = request.Appliance, Issue = request.Issue, ProblemDescription = request.ProblemDescription,
        Status = request.Status, Priority = request.Priority, AmountPaid = request.AmountPaid,
        AssignedTechnician = request.AssignedTechnician, ManagerNote = request.ManagerNote, Date = request.Date,
        AssignedTechnicianId = request.AssignedTechnicianId, CustomerUserId = request.CustomerUserId,
        ApplianceBrand = request.ApplianceBrand, ApplianceModel = request.ApplianceModel, SerialNumber = request.SerialNumber,
        CustomerPhone = request.CustomerPhone, ServiceAddress = request.ServiceAddress, Latitude = request.Latitude, Longitude = request.Longitude,
        PreferredServiceDate = request.PreferredServiceDate, NextVisitDate = request.NextVisitDate, IssueCategory = request.IssueCategory,
        TechnicianNote = request.TechnicianNote, RejectionReason = request.RejectionReason,
        TechnicianActionAt = request.TechnicianActionAt,
        AssignedParts = request.Parts.Select(link => new ServiceRequestPartDto
        {
            Id = link.Id, InventoryPartId = link.InventoryPartId, Sku = link.InventoryPart.Sku,
            Name = link.InventoryPart.Name, ApplianceType = link.InventoryPart.ApplianceType,
            Quantity = link.Quantity, AssignedAt = link.AssignedAt
        }).ToList()
    };
}
