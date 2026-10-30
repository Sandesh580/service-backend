using System.Security.Claims;
using LoginCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Controllers;

[ApiController]
[Authorize(Roles = "Technician")]
[Route("api/technician/notifications")]
public sealed class TechnicianNotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public TechnicianNotificationsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var items = await _db.ServiceNotifications.AsNoTracking()
            .Where(item => item.RecipientUserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .Select(item => new { item.Id, item.RequestNumber, item.Title, item.Message, item.CreatedAt, item.IsRead })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var item = await _db.ServiceNotifications.SingleOrDefaultAsync(notification => notification.Id == id && notification.RecipientUserId == userId, cancellationToken);
        if (item is null) return NotFound();
        item.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _db.ServiceNotifications.Where(item => item.RecipientUserId == userId && !item.IsRead)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsRead, true), cancellationToken);
        return NoContent();
    }
}