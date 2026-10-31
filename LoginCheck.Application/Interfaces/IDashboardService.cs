using LoginCheck.Application.DTOs;

namespace LoginCheck.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<IEnumerable<ServiceRequestDto>> GetRequestsAsync(string? status = null, string? search = null);
    Task<PagedResultDto<ServiceRequestDto>> GetRequestsPageAsync(string? status, string? search, int page, int pageSize);
    Task<ServiceRequestDto> CreateRequestAsync(CreateServiceRequestDto dto);
    Task<ServiceRequestDto?> UpdateStatusAsync(int id, string status, string? managerNote);
    Task<ServiceRequestDto?> UpdateAssignmentAsync(int id, string? assignedTechnician, string? managerNote, DateTime? nextVisitDate);
}
