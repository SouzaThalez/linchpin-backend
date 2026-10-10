using LinchpinBackend.DTOs;
using LinchpinBackend.Models;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

/// <summary>Endpoints comuns de Treinamentos e Disciplinas e das suas aulas.</summary>
[ApiController]
[Authorize]
public abstract class LessonGroupsController(LessonGroupService groups, LessonGroupType type) : ControllerBase
{
    private const string Editors = $"{UserRoles.Admin},{UserRoles.Analyst}";

    [HttpGet]
    public async Task<ActionResult<List<LessonGroupDto>>> List() => await groups.ListAsync(type);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonGroupDto>> Get(int id) => await groups.GetAsync(type, id);

    [Authorize(Roles = Editors)]
    [HttpPost]
    public async Task<ActionResult<LessonGroupDto>> Create(LessonGroupDto dto)
    {
        var created = await groups.CreateAsync(type, dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = Editors)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LessonGroupDto>> Update(int id, LessonGroupDto dto) => await groups.UpdateAsync(type, id, dto);

    [Authorize(Roles = Editors)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await groups.DeleteAsync(type, id);
        return NoContent();
    }

    [Authorize(Roles = Editors)]
    [HttpPost("{id:int}/lessons")]
    public async Task<ActionResult<LessonDto>> AddLesson(int id, LessonDto dto) => await groups.AddLessonAsync(type, id, dto);

    [Authorize(Roles = Editors)]
    [HttpPut("{id:int}/lessons/{lessonId:int}")]
    public async Task<ActionResult<LessonDto>> UpdateLesson(int id, int lessonId, LessonDto dto) =>
        await groups.UpdateLessonAsync(type, id, lessonId, dto);

    [Authorize(Roles = Editors)]
    [HttpDelete("{id:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int id, int lessonId)
    {
        await groups.DeleteLessonAsync(type, id, lessonId);
        return NoContent();
    }
}

[Route("api/trainings")]
public class TrainingsController(LessonGroupService groups) : LessonGroupsController(groups, LessonGroupType.Training);

[Route("api/disciplines")]
public class DisciplinesController(LessonGroupService groups) : LessonGroupsController(groups, LessonGroupType.Discipline);
