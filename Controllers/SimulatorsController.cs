using LinchpinBackend.DTOs;
using LinchpinBackend.Models;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

/// <summary>Simuladores de média e alta fidelidade (os de baixa ficam em /api/simulator-classes).</summary>
[ApiController]
[Authorize]
[Route("api/simulators")]
public class SimulatorsController(SimulatorService simulators) : ControllerBase
{
    /// <summary>?category=alta | media</summary>
    [HttpGet]
    public async Task<ActionResult<List<SimulatorDto>>> List([FromQuery] string? category) =>
        await simulators.ListAsync(category);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SimulatorDto>> Get(int id) => await simulators.GetAsync(id);

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpPost]
    public async Task<ActionResult<SimulatorDto>> Create(SimulatorDto dto)
    {
        var created = await simulators.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Substitui o simulador inteiro, inclusive a lista de códigos.</summary>
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SimulatorDto>> Update(int id, SimulatorDto dto) => await simulators.UpdateAsync(id, dto);

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await simulators.DeleteAsync(id);
        return NoContent();
    }
}
