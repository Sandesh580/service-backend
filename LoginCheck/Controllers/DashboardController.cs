using LoginCheck.Application.DTOs;
using LoginCheck.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LoginCheck.Controllers;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        if (page.HasValue || pageSize.HasValue)
        {
            var result = await _dashboardService.GetRequestsPageAsync(status, search,
                Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? 10, 1, 50));
            return Ok(result);
        }
        var requests = await _dashboardService.GetRequestsAsync(status, search);
        return Ok(requests);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateServiceRequestDto dto)
    {
        var result = await _dashboardService.CreateRequestAsync(dto);
        return CreatedAtAction(nameof(GetRequests), result);
    }

    [HttpPatch("requests/{id}/assignment")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentDto dto)
    {
        try
        {
            var result = await _dashboardService.UpdateAssignmentAsync(id, dto.AssignedTechnician, dto.ManagerNote);
            if (result is null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("requests/{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var result = await _dashboardService.UpdateStatusAsync(id, dto.Status, dto.ManagerNote);
        if (result is null) return NotFound();
        return Ok(result);
    }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.StringLength(2000)] public string? ManagerNote { get; set; }
}

public class UpdateAssignmentDto
{
    public string? AssignedTechnician { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(2000)] public string? ManagerNote { get; set; }
}
