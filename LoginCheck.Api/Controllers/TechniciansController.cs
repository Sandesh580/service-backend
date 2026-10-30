using LoginCheck.Application.DTOs;
using LoginCheck.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoginCheck.Api.Controllers;

[ApiController]
[Route("api/admin/technicians")]
public sealed class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _technicianService;

    public TechniciansController(ITechnicianService technicianService)
    {
        _technicianService = technicianService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TechnicianResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _technicianService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TechnicianResponseDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var technician = await _technicianService.GetByIdAsync(id, cancellationToken);
        return technician is null ? NotFound() : Ok(technician);
    }

    [HttpPost]
    public async Task<ActionResult<TechnicianResponseDto>> Create(
        CreateTechnicianDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = technician.Id }, technician);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TechnicianResponseDto>> Update(
        int id,
        UpdateTechnicianDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var technician = await _technicianService.UpdateAsync(id, dto, cancellationToken);
            return technician is null ? NotFound() : Ok(technician);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _technicianService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
