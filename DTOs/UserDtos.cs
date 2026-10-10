using LinchpinBackend.Models;

namespace LinchpinBackend.DTOs;

/// <summary>Usuário devolvido pela API. Nunca inclui a senha.</summary>
public record UserDto(int Id, string Name, string Login, string Email, string? Image, string Role, string Task)
{
    public static UserDto From(User u) => new(u.Id, u.Name, u.Login, u.Email, u.Image, u.Role, u.Task);
}

public class UserCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Task { get; set; }
}

/// <summary>Atualização parcial: campos nulos ou vazios mantêm o valor atual.</summary>
public class UserUpdateDto
{
    public string? Name { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? Email { get; set; }
    public string? Image { get; set; }
    public string? Role { get; set; }
    public string? Task { get; set; }
}
