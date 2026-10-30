using System.Linq.Expressions;
using LoginCheck.Application.DTOs;
using LoginCheck.Application.Interfaces;
using LoginCheck.Domain.Entities;
using LoginCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoginCheck.Infrastructure.Services;

public sealed class TechnicianService : ITechnicianService
{
    private const string TechnicianRole = "Technician";
    private readonly ApplicationDbContext _context;

    public TechnicianService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TechnicianResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.Role == TechnicianRole)
            .OrderBy(user => user.UserName)
            .Select(ToResponse())
            .ToListAsync(cancellationToken);
    }

    public async Task<TechnicianResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == id && user.Role == TechnicianRole)
            .Select(ToResponse())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TechnicianResponseDto> CreateAsync(
        CreateTechnicianDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim();
        if (await _context.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var technician = new User
        {
            UserName = dto.Name.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            Specialties = dto.Specialties.Trim(),
            EducationQualification = dto.EducationQualification.Trim(),
            VocationalTraining = dto.VocationalTraining.Trim(),
            PermanentAddress = dto.PermanentAddress.Trim(),
            CurrentAddress = dto.CurrentAddress.Trim(),
            BloodGroup = dto.BloodGroup.Trim(),
            PastExperience = dto.PastExperience.Trim(),
            Role = TechnicianRole,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(technician);
        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(technician);
    }

    public async Task<TechnicianResponseDto?> UpdateAsync(
        int id,
        UpdateTechnicianDto dto,
        CancellationToken cancellationToken = default)
    {
        var technician = await _context.Users
            .SingleOrDefaultAsync(user => user.Id == id && user.Role == TechnicianRole, cancellationToken);

        if (technician is null)
        {
            return null;
        }

        var email = dto.Email.Trim();
        var duplicateEmail = await _context.Users.AnyAsync(
            user => user.Id != id && user.Email == email,
            cancellationToken);

        if (duplicateEmail)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var previousName = technician.UserName;
        technician.UserName = dto.Name.Trim();
        technician.Email = email;
        technician.Phone = dto.Phone.Trim();
        technician.Specialties = dto.Specialties.Trim();
        technician.EducationQualification = dto.EducationQualification.Trim();
        technician.VocationalTraining = dto.VocationalTraining.Trim();
        technician.PermanentAddress = dto.PermanentAddress.Trim();
        technician.CurrentAddress = dto.CurrentAddress.Trim();
        technician.BloodGroup = dto.BloodGroup.Trim();
        technician.PastExperience = dto.PastExperience.Trim();
        if (!string.Equals(previousName, technician.UserName, StringComparison.Ordinal))
        {
            var assignedRequests = await _context.ServiceRequests
                .Where(request => request.AssignedTechnicianId == technician.Id)
                .ToListAsync(cancellationToken);
            foreach (var request in assignedRequests) request.AssignedTechnician = technician.UserName;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            technician.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ToResponse(technician);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var technician = await _context.Users
            .SingleOrDefaultAsync(user => user.Id == id && user.Role == TechnicianRole, cancellationToken);

        if (technician is null)
        {
            return false;
        }

        if (await _context.ServiceRequests.AnyAsync(request => request.AssignedTechnicianId == id, cancellationToken))
        {
            throw new InvalidOperationException("This technician has service history. Reassign open calls before removing the account.");
        }

        _context.Users.Remove(technician);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TechnicianResponseDto ToResponse(User user)
    {
        return new TechnicianResponseDto
        {
            Id = user.Id,
            Name = user.UserName,
            Email = user.Email,
            Phone = user.Phone,
            Specialties = user.Specialties,
            EducationQualification = user.EducationQualification,
            VocationalTraining = user.VocationalTraining,
            PermanentAddress = user.PermanentAddress,
            CurrentAddress = user.CurrentAddress,
            BloodGroup = user.BloodGroup,
            PastExperience = user.PastExperience,
            Role = user.Role
        };
    }

    private static Expression<Func<User, TechnicianResponseDto>> ToResponse()
    {
        return user => new TechnicianResponseDto
        {
            Id = user.Id,
            Name = user.UserName,
            Email = user.Email,
            Phone = user.Phone,
            Specialties = user.Specialties,
            EducationQualification = user.EducationQualification,
            VocationalTraining = user.VocationalTraining,
            PermanentAddress = user.PermanentAddress,
            CurrentAddress = user.CurrentAddress,
            BloodGroup = user.BloodGroup,
            PastExperience = user.PastExperience,
            Role = user.Role
        };
    }
}
