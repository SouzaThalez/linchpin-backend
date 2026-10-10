using LinchpinBackend.DTOs;
using LinchpinBackend.Models;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

/// <summary>Classes de simuladores de baixa fidelidade, com seus simuladores.</summary>
[ApiController]
[Authorize]
[Route("api/simulator-classes")]
public class SimulatorClassesController(SimulatorClassService classes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SimulatorClassDto>>> List() => await classes.ListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SimulatorClassDto>> Get(int id) => await classes.GetAsync(id);

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpPost]
    public async Task<ActionResult<SimulatorClassDto>> Create(SimulatorClassDto dto)
    {
        var created = await classes.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Substitui a classe inteira, inclusive a lista de simuladores e códigos.</summary>
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SimulatorClassDto>> Update(int id, SimulatorClassDto dto) => await classes.UpdateAsync(id, dto);

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await classes.DeleteAsync(id);
        return NoContent();
    }
}
