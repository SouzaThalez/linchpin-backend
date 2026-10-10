using System.Security.Claims;
using LinchpinBackend.Models;

namespace LinchpinBackend.Infrastructure;

/// <summary>Dados do usuário autenticado, lidos do token JWT.</summary>
public interface ICurrentUser
{
    int? Id { get; }
    string? Name { get; }
    string? Role { get; }
    bool IsAdmin { get; }
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string? Name => Principal?.FindFirstValue(ClaimTypes.Name);
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public bool IsAdmin => Role == UserRoles.Admin;
}
