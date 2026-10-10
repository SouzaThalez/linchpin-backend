using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth, UserService users, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Valida login e senha e devolve o token JWT e os dados do usuário.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) => await auth.LoginAsync(request);

    /// <summary>Usuário dono do token.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me() => await users.GetAsync(currentUser.Id!.Value);
}
