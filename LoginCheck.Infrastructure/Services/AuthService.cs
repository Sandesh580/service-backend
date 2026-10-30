using LoginCheck.Application.DTOs;
using LoginCheck.Application.Interfaces;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(ApplicationDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) ||
            string.IsNullOrWhiteSpace(dto.Phone) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new InvalidOperationException("Full name, 10-digit phone number, and password are required.");
        }

        var phone = dto.Phone.Trim();
        if (phone.Length != 10 || phone.Any(character => !char.IsDigit(character)))
            throw new InvalidOperationException("Enter a valid 10-digit phone number.");
        var email = dto.Email.Trim();
        var userName = dto.UserName.Trim();
        if (string.IsNullOrWhiteSpace(email)) email = $"phone-{phone}@accounts.dhc.local";
        var exists = await _context.Users.AnyAsync(x => x.Email == email || x.Phone == phone || x.Phone == $"+977{phone}");
        if (exists)
        {
            throw new InvalidOperationException("An account already uses this phone number or email address.");
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            Phone = $"+977{phone}",
            Address = dto.Address.Trim(),
            CurrentAddress = dto.Address.Trim(),
            AvatarKey = dto.AvatarKey is "avatar-1" or "avatar-2" or "avatar-3" or "avatar-4" or "avatar-5" or "avatar-6" ? dto.AvatarKey : "avatar-1",
            Role = "Customer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = _jwtTokenService.GenerateToken(user.UserName, user.Email, user.Id, NormalizeRole(user.Role)),
            UserName = user.UserName,
            UserId = user.Id,
            Role = NormalizeRole(user.Role),
            AvatarKey = user.AvatarKey
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var loginId = dto.Email.Trim();
        var technicianLogin = loginId.StartsWith("TECH-", StringComparison.OrdinalIgnoreCase) ? loginId[5..] : loginId;
        User? user = int.TryParse(technicianLogin, out var technicianId)
            ? await _context.Users.FirstOrDefaultAsync(x => x.Id == technicianId && x.Role == "Technician")
            : await _context.Users.FirstOrDefaultAsync(x => x.Email == loginId || (x.Role == "Customer" && (x.Phone == loginId || x.Phone == $"+977{loginId}")));
        if (user is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        return new AuthResponseDto
        {
            Token = _jwtTokenService.GenerateToken(user.UserName, user.Email, user.Id, NormalizeRole(user.Role)),
            UserName = user.UserName,
            UserId = user.Id,
            Role = NormalizeRole(user.Role),
            AvatarKey = user.AvatarKey
        };
    }

    private static string NormalizeRole(string role) =>
        string.Equals(role, "User", StringComparison.OrdinalIgnoreCase) ? "Customer" : role;
}
