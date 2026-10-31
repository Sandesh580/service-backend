using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LoginCheck.Application.DTOs;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Controllers;

[ApiController]
[Authorize(Roles = "Technician")]
[Route("api/technician/requests")]
public sealed class TechnicianRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public TechnicianRequestsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAssignedRequests(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var requests = (await _db.ServiceRequests.AsNoTracking().Include(request => request.Parts).ThenInclude(link => link.InventoryPart)
            .Where(request => request.AssignedTechnicianId == userId)
            .OrderByDescending(request => request.TechnicianActionAt ?? request.Date)
            .ToListAsync(cancellationToken)).Select(ToDto);
        return Ok(requests);
    }

    [HttpPatch("{id:int}/action")]
    public async Task<IActionResult> UpdateAssignedRequest(int id, [FromBody] TechnicianActionDto dto, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var action = dto.Action.Trim().ToLowerInvariant();
        var known = new[] { "accept", "reject", "update", "parts", "followup", "resume", "return", "complete" };
        if (!known.Contains(action)) return BadRequest(new { message = "Choose a valid call action." });
        if (action is "reject" or "update" or "parts" or "followup" or "return" or "complete" && string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { message = "Add a reason or work update before saving." });
        if (action == "complete" && dto.Reason!.Trim().Length < 8)
            return BadRequest(new { message = "Add at least 8 characters describing the completed repair." });
        if (action == "complete" && dto.AmountPaid is null)
            return BadRequest(new { message = "Enter the amount paid. Enter 0 for no-charge work." });
        if (action == "followup" && dto.NextVisitDate is null)
            return BadRequest(new { message = "Choose the date for the next visit." });
        if (dto.NextVisitDate is { } date && date.Date < DateTime.UtcNow.Date)
            return BadRequest(new { message = "The next visit date cannot be in the past." });

        var request = await _db.ServiceRequests.Include(item => item.Parts).ThenInclude(link => link.InventoryPart)
            .SingleOrDefaultAsync(item => item.Id == id && item.AssignedTechnicianId == userId, cancellationToken);
        if (request is null) return NotFound(new { message = "This call is not assigned to your technician account." });
        var initial = request.Status is "New" or "Scheduled" or "PendingApproval";
        var acceptedWork = request.Status is "InProgress" or "AwaitingParts" or "TechnicianPending";
        var allowed = action switch
        {
            "accept" or "reject" => initial,
            "update" or "parts" or "followup" or "complete" => request.Status == "InProgress",
            "resume" => request.Status is "AwaitingParts" or "TechnicianPending",
            "return" => acceptedWork,
            _ => false
        };
        if (!allowed) return Conflict(new { message = "That action is not available for the call’s current status. Refresh your calls and try again." });

        request.Status = action switch
        {
            "accept" or "update" or "resume" => "InProgress",
            "parts" => "AwaitingParts",
            "followup" => "TechnicianPending",
            "return" => "PendingApproval",
            "complete" => "Completed",
            _ => "Rejected"
        };
        if (action == "complete") request.AmountPaid = dto.AmountPaid;
        if (action == "followup") request.NextVisitDate = dto.NextVisitDate!.Value.ToUniversalTime();
        if (action == "resume") request.NextVisitDate = null;
        if (action == "return") { request.AssignedTechnicianId = null; request.AssignedTechnician = null; }
        request.TechnicianNote = dto.Reason?.Trim() ?? (action == "accept" ? "Call accepted." : action == "resume" ? "Work resumed." : string.Empty);
        request.RejectionReason = action == "reject" ? request.TechnicianNote : string.Empty;
        request.TechnicianActionAt = DateTime.UtcNow;
        var statusLabel = action switch { "accept" => "Call accepted", "reject" => "Call rejected", "update" => "Work update added", "parts" => "Waiting for parts", "followup" => "Follow-up visit scheduled", "resume" => "Work resumed", "return" => "Call returned to manager", _ => "Call completed" };
        _db.ServiceNotifications.Add(new ServiceNotification
        {
            RecipientUserId = userId, ServiceRequestId = request.Id, RequestNumber = request.RequestNumber,
            Title = statusLabel,
            Message = $"Request #{request.RequestNumber}: {statusLabel}." + (string.IsNullOrWhiteSpace(request.TechnicianNote) ? "" : $" Technician remark: {request.TechnicianNote}") + (request.NextVisitDate is { } next ? $" Next visit: {next.ToString("d")}." : ""),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(request));
    }

    private static ServiceRequestDto ToDto(ServiceRequest request) => new()
    {
        Id = request.Id, RequestNumber = request.RequestNumber, CustomerName = request.CustomerName,
        Appliance = request.Appliance, Issue = request.Issue, ProblemDescription = request.ProblemDescription, Status = request.Status,
        AmountPaid = request.AmountPaid, AssignedTechnician = request.AssignedTechnician, ManagerNote = request.ManagerNote, Date = request.Date,
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

public sealed class TechnicianActionDto
{
    [Required] public string Action { get; set; } = string.Empty;
    [StringLength(2000)] public string? Reason { get; set; }
    [Range(typeof(decimal), "0", "999999999.99")] public decimal? AmountPaid { get; set; }
    public DateTime? NextVisitDate { get; set; }
}
