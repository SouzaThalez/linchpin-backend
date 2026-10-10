using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

public class AuthService(AppDbContext db, TokenService tokens)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            throw new AppException("Informe usuário e senha.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Login == request.Login.Trim());

        // Mensagem única para não revelar se o login existe.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppException("Usuário ou senha incorretos.", StatusCodes.Status401Unauthorized);

        var (token, expiresAt) = tokens.Create(user);
        return new LoginResponse(token, expiresAt, UserDto.From(user));
    }
}
