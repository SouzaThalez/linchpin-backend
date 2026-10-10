using System.Text.Json.Nodes;
using LinchpinBackend.Models;
using LinchpinBackend.Models.Reports;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

/// <summary>
/// Endpoints comuns dos relatórios. Filtros via query string sobre os campos do relatório,
/// ex.: GET /api/cleaning-reports?hasDescription=true&amp;user.name=Thales
/// </summary>
[ApiController]
[Authorize]
public abstract class ReportsController<T>(ReportService<T> reports) : ControllerBase where T : Report, new()
{
    [HttpGet]
    public async Task<ActionResult<List<JsonObject>>> List()
    {
        var filters = Request.Query
            .Where(q => !q.Key.StartsWith('_'))
            .ToDictionary(q => q.Key, q => q.Value.ToString());
        return await reports.ListAsync(filters);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JsonObject>> Get(int id) => await reports.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<JsonObject>> Create(JsonObject document)
    {
        var created = await reports.CreateAsync(document);
        return CreatedAtAction(nameof(Get), new { id = (int)created["id"]! }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<JsonObject>> Replace(int id, JsonObject document) => await reports.ReplaceAsync(id, document);

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<JsonObject>> Patch(int id, JsonObject changes) => await reports.PatchAsync(id, changes);

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await reports.DeleteAsync(id);
        return NoContent();
    }
}

[Route("api/lesson-reports")]
public class LessonReportsController(ReportService<LessonReport> reports) : ReportsController<LessonReport>(reports);

[Route("api/scenario-reports")]
public class ScenarioReportsController(ReportService<ScenarioReport> reports) : ReportsController<ScenarioReport>(reports);

[Route("api/cleaning-reports")]
public class CleaningReportsController(ReportService<CleaningReport> reports) : ReportsController<CleaningReport>(reports);

[Route("api/maintenance-reports")]
public class MaintenanceReportsController(ReportService<MaintenanceReport> reports) : ReportsController<MaintenanceReport>(reports);
