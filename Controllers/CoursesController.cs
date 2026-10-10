using LinchpinBackend.DTOs;
using LinchpinBackend.Models;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/courses")]
public class CoursesController(CourseService courses) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> List() => await courses.ListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> Get(int id) => await courses.GetAsync(id);

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CourseDto dto)
    {
        var created = await courses.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseDto>> Update(int id, CourseDto dto) => await courses.UpdateAsync(id, dto);

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Analyst}")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await courses.DeleteAsync(id);
        return NoContent();
    }
}
