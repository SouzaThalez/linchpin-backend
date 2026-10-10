using LinchpinBackend.DTOs;
using LinchpinBackend.Models;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

/// <summary>
/// Relatórios de limpeza. Filtros opcionais via query string,
/// ex.: GET /api/cleaning-reports?simulatorCategory=alta&amp;hasDescription=true&amp;user.name=Thales
/// </summary>
[ApiController]
[Authorize]
[Route("api/cleaning-reports")]
public class CleaningReportsController(CleaningReportService reports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CleaningReportDto>>> List([FromQuery] CleaningReportFilter filter) =>
        await reports.ListAsync(filter);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CleaningReportDto>> Get(int id) => await reports.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<CleaningReportDto>> Create(CleaningReportRequest request)
    {
        var created = await reports.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CleaningReportDto>> Update(int id, CleaningReportRequest request) =>
        await reports.UpdateAsync(id, request);

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await reports.DeleteAsync(id);
        return NoContent();
    }
}
