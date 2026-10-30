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
[Authorize(Roles = "Customer")]
[Route("api/customer")]
public sealed class CustomerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CustomerController(ApplicationDbContext db) => _db = db;

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var customer = await _db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (customer is null) return Unauthorized();
        return Ok(new { name = customer.UserName, phone = customer.Phone, address = string.IsNullOrWhiteSpace(customer.Address) ? customer.CurrentAddress : customer.Address, email = customer.Email.EndsWith("@accounts.dhc.local", StringComparison.OrdinalIgnoreCase) ? string.Empty : customer.Email, avatarKey = customer.AvatarKey });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var customer = await _db.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (customer is null) return Unauthorized();
        customer.UserName = dto.Name.Trim();
        customer.Address = dto.Address.Trim();
        customer.CurrentAddress = dto.Address.Trim();
        if (dto.AvatarKey is "avatar-1" or "avatar-2" or "avatar-3" or "avatar-4" or "avatar-5" or "avatar-6") customer.AvatarKey = dto.AvatarKey;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { name = customer.UserName, phone = customer.Phone, address = customer.Address, email = customer.Email.EndsWith("@accounts.dhc.local", StringComparison.OrdinalIgnoreCase) ? string.Empty : customer.Email, avatarKey = customer.AvatarKey });
    }

    [HttpGet("appliances")]
    public IActionResult GetAppliances() => Ok(new[]
    {
        new { id = "refrigerator", name = "Refrigerator", icon = "🧊", summary = "Cooling, leaks, ice maker, unusual noise", checks = new[] { "Cooling and temperature", "Water and ice systems", "Doors, seals and unusual sounds" } },
        new { id = "washer", name = "Washing machine", icon = "🧺", summary = "Drainage, spin cycle, leaks and error codes", checks = new[] { "Water supply and drainage", "Spin and vibration", "Door lock and error codes" } },
        new { id = "dryer", name = "Clothes dryer", icon = "♨️", summary = "Heating, airflow, drum and cycle issues", checks = new[] { "Heating and airflow", "Drum and belt", "Controls and cycle time" } },
        new { id = "dishwasher", name = "Dishwasher", icon = "🍽️", summary = "Cleaning, draining, leaks and door latch", checks = new[] { "Wash performance", "Drainage and leaks", "Door and controls" } },
        new { id = "oven", name = "Oven or range", icon = "🍳", summary = "Heating, burners, controls and safety checks", checks = new[] { "Temperature and heating", "Burners and ignition", "Door, controls and safety" } },
        new { id = "air-conditioner", name = "Air conditioner", icon = "❄️", summary = "Cooling, airflow, thermostat and water leaks", checks = new[] { "Cooling and airflow", "Thermostat and controls", "Drainage and unusual noise" } },
        new { id = "microwave", name = "Microwave", icon = "📡", summary = "Heating, turntable, controls and door", checks = new[] { "Heating performance", "Turntable and controls", "Door and latch" } },
        new { id = "water-heater", name = "Water heater", icon = "🚿", summary = "Hot water, temperature, leaks and noises", checks = new[] { "Water temperature", "Leaks and pressure", "Heating and unusual sounds" } }
    });

    [HttpGet("requests")]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var requests = await _db.ServiceRequests.AsNoTracking()
            .Where(request => request.CustomerUserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new ServiceRequestDto
            {
                Id = request.Id, RequestNumber = request.RequestNumber, CustomerName = request.CustomerName,
                Appliance = request.Appliance, Issue = request.Issue, ProblemDescription = request.ProblemDescription, Status = request.Status,
                Priority = request.Priority, AmountPaid = request.AmountPaid, AssignedTechnician = request.AssignedTechnician,
                AssignedTechnicianPhone = request.Status != "Completed" && request.Status != "Rejected"
                    ? _db.Users
                    .Where(technician => technician.Role == "Technician" &&
                        (technician.Id == request.AssignedTechnicianId ||
                         (request.AssignedTechnicianId == null && technician.UserName == request.AssignedTechnician)))
                    .Select(technician => technician.Phone)
                    .FirstOrDefault() : null,
                Date = request.Date,
                ApplianceBrand = request.ApplianceBrand, ApplianceModel = request.ApplianceModel,
                SerialNumber = request.SerialNumber, CustomerPhone = request.CustomerPhone,
                ServiceAddress = request.ServiceAddress, Latitude = request.Latitude, Longitude = request.Longitude,
                PreferredServiceDate = request.PreferredServiceDate, NextVisitDate = request.NextVisitDate,
                IssueCategory = request.IssueCategory, TechnicianNote = request.TechnicianNote,
                RejectionReason = request.RejectionReason, TechnicianActionAt = request.TechnicianActionAt
            }).ToListAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> BookRequest([FromBody] BookServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var customer = await _db.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (customer is null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (dto.PreferredServiceDate is { } requestedDate && requestedDate.Date < DateTime.Today.Date)
            return BadRequest(new { message = "Choose today or a future preferred service date." });
        if (dto.Latitude is null || dto.Longitude is null)
            return BadRequest(new { message = "Choose your current service location so the technician can get directions." });

        var previous = await _db.ServiceRequests.OrderByDescending(item => item.Id).Select(item => item.RequestNumber).FirstOrDefaultAsync(cancellationToken);
        var next = int.TryParse(previous, out var priorNumber) ? priorNumber + 1 : 2401;
        var type = dto.ApplianceType.Trim();
        var brand = dto.ApplianceBrand.Trim();
        var model = dto.ApplianceModel.Trim();
        var appliance = string.Join(" ", new[] { brand, model }.Where(part => !string.IsNullOrWhiteSpace(part)));
        appliance = string.IsNullOrWhiteSpace(appliance) ? type : $"{type} · {appliance}";
        var category = dto.IssueCategory.Trim();
        var description = (dto.Description ?? string.Empty).Trim();
        var issue = string.IsNullOrWhiteSpace(description) ? category : $"{category}: {description}";
        if (appliance.Length > 100) appliance = appliance[..100];
        if (issue.Length > 500) issue = issue[..500];

        var request = new ServiceRequest
        {
            RequestNumber = next.ToString(), CustomerName = customer.UserName,
            Appliance = appliance, Issue = issue, ProblemDescription = description, Status = "New", Priority = "-",
            Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, CustomerUserId = userId,
            ApplianceBrand = brand, ApplianceModel = model, SerialNumber = dto.SerialNumber.Trim(),
            CustomerPhone = dto.CustomerPhone.Trim(), ServiceAddress = dto.ServiceAddress.Trim(),
            Latitude = dto.Latitude, Longitude = dto.Longitude,
            PreferredServiceDate = dto.PreferredServiceDate is { } visitDate ? DateTime.SpecifyKind(visitDate.Date, DateTimeKind.Unspecified) : null,
            IssueCategory = category
        };
        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return Created("/api/customer/requests", new
        {
            request.Id, request.RequestNumber, request.Appliance, request.Issue, request.Status,
            request.Priority, request.Date, request.PreferredServiceDate, request.Latitude, request.Longitude
        });
    }

    private int? GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

}

public sealed class UpdateCustomerProfileDto
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(1000)] public string Address { get; set; } = string.Empty;
    [StringLength(32)] public string AvatarKey { get; set; } = "avatar-1";
}

public sealed class BookServiceRequestDto
{
    [Required, StringLength(100)] public string ApplianceType { get; set; } = string.Empty;
    [StringLength(100)] public string ApplianceBrand { get; set; } = string.Empty;
    [StringLength(100)] public string ApplianceModel { get; set; } = string.Empty;
    [StringLength(100)] public string SerialNumber { get; set; } = string.Empty;
    [Required, StringLength(100)] public string IssueCategory { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required, Phone, StringLength(40)] public string CustomerPhone { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(500)] public string ServiceAddress { get; set; } = string.Empty;
    [Required] public DateTime? PreferredServiceDate { get; set; }
    [Required, Range(-90d, 90d)] public double? Latitude { get; set; }
    [Required, Range(-180d, 180d)] public double? Longitude { get; set; }
}
