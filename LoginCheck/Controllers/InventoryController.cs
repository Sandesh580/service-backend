using System.ComponentModel.DataAnnotations;
using LoginCheck.Application.DTOs;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Controllers;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public InventoryController(ApplicationDbContext db) => _db = db;

    [HttpGet("parts")]
    public async Task<IActionResult> GetParts([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _db.InventoryParts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(part => part.Sku.Contains(term) || part.Name.Contains(term) || part.ApplianceType.Contains(term));
        }
        var parts = await query.OrderBy(part => part.ApplianceType).ThenBy(part => part.Name).Select(part => new InventoryPartDto
        {
            Id = part.Id, Sku = part.Sku, Name = part.Name, ApplianceType = part.ApplianceType,
            Description = part.Description, QuantityInStock = part.QuantityInStock, UnitPrice = part.UnitPrice
        }).ToListAsync(cancellationToken);
        return Ok(parts);
    }

    [HttpPost("parts")]
    public async Task<IActionResult> AddPart([FromBody] CreateInventoryPartDto dto, CancellationToken cancellationToken)
    {
        var sku = dto.Sku.Trim();
        if (await _db.InventoryParts.AnyAsync(part => part.Sku == sku, cancellationToken))
            return Conflict(new { message = "A part with this SKU already exists." });
        var part = new InventoryPart
        {
            Sku = sku, Name = dto.Name.Trim(), ApplianceType = dto.ApplianceType.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            QuantityInStock = dto.QuantityInStock, UnitPrice = dto.UnitPrice
        };
        _db.InventoryParts.Add(part);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(part));
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> AssignPart([FromBody] AssignInventoryPartDto dto, CancellationToken cancellationToken)
    {
        if (dto.Quantity < 1) return BadRequest(new { message = "Choose at least one part to assign." });
        var request = await _db.ServiceRequests.SingleOrDefaultAsync(item => item.Id == dto.ServiceRequestId, cancellationToken);
        if (request is null) return NotFound(new { message = "The selected service call was not found." });
        if (request.Status != "AwaitingParts")
            return Conflict(new { message = "Parts can only be assigned to a call that is waiting for parts." });
        var part = await _db.InventoryParts.SingleOrDefaultAsync(item => item.Id == dto.InventoryPartId, cancellationToken);
        if (part is null) return NotFound(new { message = "The selected inventory part was not found." });
        if (part.QuantityInStock < dto.Quantity)
            return Conflict(new { message = $"Only {part.QuantityInStock} of this part are currently in stock." });

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await _db.ServiceRequestParts.SingleOrDefaultAsync(link => link.ServiceRequestId == request.Id && link.InventoryPartId == part.Id, cancellationToken);
        if (existing is null)
        {
            _db.ServiceRequestParts.Add(new ServiceRequestPart
            {
                ServiceRequestId = request.Id, InventoryPartId = part.Id, Quantity = dto.Quantity, AssignedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Quantity += dto.Quantity;
            existing.AssignedAt = DateTime.UtcNow;
        }
        part.QuantityInStock -= dto.Quantity;

        if (request.AssignedTechnicianId is { } technicianId)
        {
            _db.ServiceNotifications.Add(new ServiceNotification
            {
                RecipientUserId = technicianId, ServiceRequestId = request.Id, RequestNumber = request.RequestNumber,
                Title = "Part assigned to your call",
                Message = $"{dto.Quantity} × {part.Name} ({part.Sku}) has been reserved for request #{request.RequestNumber}. The call is still waiting for you to resume work.",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = $"{dto.Quantity} × {part.Name} assigned to request #{request.RequestNumber}.", part = ToDto(part) });
    }

    private static InventoryPartDto ToDto(InventoryPart part) => new()
    {
        Id = part.Id, Sku = part.Sku, Name = part.Name, ApplianceType = part.ApplianceType,
        Description = part.Description, QuantityInStock = part.QuantityInStock, UnitPrice = part.UnitPrice
    };
}

public sealed class CreateInventoryPartDto
{
    [Required, StringLength(40)] public string Sku { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(80)] public string ApplianceType { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Range(0, 1000000)] public int QuantityInStock { get; set; }
    [Range(typeof(decimal), "0", "99999999.99")] public decimal UnitPrice { get; set; }
}

public sealed class AssignInventoryPartDto
{
    [Range(1, int.MaxValue)] public int ServiceRequestId { get; set; }
    [Range(1, int.MaxValue)] public int InventoryPartId { get; set; }
    [Range(1, 100000)] public int Quantity { get; set; } = 1;
}

public sealed class InventoryPartDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApplianceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public decimal UnitPrice { get; set; }
}
