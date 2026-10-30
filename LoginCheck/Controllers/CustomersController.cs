using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Controllers;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/customers")]
public sealed class CustomersController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var customers = await db.Users.AsNoTracking().Where(user => user.Role == "Customer")
            .OrderBy(user => user.UserName)
            .Select(user => new CustomerRecordDto
            {
                Id = user.Id, Name = user.UserName, Phone = user.Phone,
                Email = user.Email.EndsWith("@accounts.dhc.local") ? string.Empty : user.Email,
                Address = string.IsNullOrWhiteSpace(user.Address) ? user.CurrentAddress : user.Address,
                AvatarKey = user.AvatarKey
            }).ToListAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRecordDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == id && item.Role == "Customer", cancellationToken);
        if (user is null) return NotFound(new { message = "Customer account was not found." });
        if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password.Length < 8)
            return BadRequest(new { message = "A new password must contain at least 8 characters." });
        user.UserName = dto.Name.Trim();
        user.Address = dto.Address.Trim();
        user.CurrentAddress = dto.Address.Trim();
        if (dto.AvatarKey is "avatar-1" or "avatar-2" or "avatar-3" or "avatar-4" or "avatar-5" or "avatar-6") user.AvatarKey = dto.AvatarKey;
        if (!string.IsNullOrWhiteSpace(dto.Password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new CustomerRecordDto
        {
            Id = user.Id, Name = user.UserName, Phone = user.Phone,
            Email = user.Email.EndsWith("@accounts.dhc.local") ? string.Empty : user.Email,
            Address = user.Address, AvatarKey = user.AvatarKey
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == id && item.Role == "Customer", cancellationToken);
        if (user is null) return NotFound(new { message = "Customer account was not found." });
        var requests = await db.ServiceRequests.Where(request => request.CustomerUserId == id).ToListAsync(cancellationToken);
        foreach (var request in requests) request.CustomerUserId = null;
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class CustomerRecordDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string AvatarKey { get; set; } = "avatar-1";
}

public sealed class UpdateCustomerRecordDto
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Address { get; set; } = string.Empty;
    [StringLength(32)] public string AvatarKey { get; set; } = "avatar-1";
    [StringLength(100)] public string? Password { get; set; }
}
