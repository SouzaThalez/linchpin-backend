using LinchpinBackend.DTOs;
using LinchpinBackend.Models;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(UserService users) : ControllerBase
{
    /// <summary>Lista os usuários, opcionalmente filtrando pelo perfil (?role=analista).</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> List([FromQuery] string? role) => await users.ListAsync(role);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id) => await users.GetAsync(id);

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreateDto dto)
    {
        var created = await users.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Administrador altera qualquer usuário; os demais apenas o próprio perfil.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UserUpdateDto dto) => await users.UpdateAsync(id, dto);

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await users.DeleteAsync(id);
        return NoContent();
    }
}
