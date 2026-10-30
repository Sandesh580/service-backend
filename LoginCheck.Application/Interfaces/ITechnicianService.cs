using LoginCheck.Application.DTOs;

namespace LoginCheck.Application.Interfaces;

public interface ITechnicianService
{
    Task<IReadOnlyList<TechnicianResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TechnicianResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TechnicianResponseDto> CreateAsync(CreateTechnicianDto dto, CancellationToken cancellationToken = default);
    Task<TechnicianResponseDto?> UpdateAsync(int id, UpdateTechnicianDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
