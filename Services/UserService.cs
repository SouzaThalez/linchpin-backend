using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

public class UserService(AppDbContext db, ICurrentUser currentUser)
{
    private const int MinPasswordLength = 6;

    public async Task<List<UserDto>> ListAsync(string? role)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var users = await query.OrderBy(u => u.Name).ToListAsync();
        return users.Select(UserDto.From).ToList();
    }

    public async Task<UserDto> GetAsync(int id) => UserDto.From(await FindAsync(id));

    public async Task<UserDto> CreateAsync(UserCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Email))
            throw new AppException("Nome, login e e-mail são obrigatórios.");
        if (!UserRoles.IsValid(dto.Role))
            throw new AppException($"Perfil inválido. Use: {string.Join(", ", UserRoles.All)}.");

        ValidatePassword(dto.Password, dto.ConfirmPassword);
        await EnsureLoginIsFreeAsync(dto.Login.Trim());

        var user = new User
        {
            Name = dto.Name.Trim(),
            Login = dto.Login.Trim(),
            Email = dto.Email.Trim(),
            Role = dto.Role,
            Image = string.IsNullOrWhiteSpace(dto.Image) ? UserRoles.DefaultImage(dto.Role) : dto.Image,
            Task = NormalizeTask(dto.Role, dto.Task),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return UserDto.From(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UserUpdateDto dto)
    {
        // Administrador edita qualquer usuário; os demais só o próprio perfil.
        if (!currentUser.IsAdmin && currentUser.Id != id)
            throw new ForbiddenException("Você só pode alterar o seu próprio perfil.");

        var user = await FindAsync(id);

        if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Image)) user.Image = dto.Image;

        if (!string.IsNullOrWhiteSpace(dto.Login) && dto.Login.Trim() != user.Login)
        {
            await EnsureLoginIsFreeAsync(dto.Login.Trim(), id);
            user.Login = dto.Login.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Role) && dto.Role != user.Role)
        {
            if (!currentUser.IsAdmin)
                throw new ForbiddenException("Somente administradores podem alterar o perfil de acesso.");
            if (!UserRoles.IsValid(dto.Role))
                throw new AppException($"Perfil inválido. Use: {string.Join(", ", UserRoles.All)}.");
            user.Role = dto.Role;
        }

        if (dto.Task is not null || dto.Role is not null)
            user.Task = NormalizeTask(user.Role, dto.Task ?? user.Task);

        if (!string.IsNullOrEmpty(dto.Password))
        {
            ValidatePassword(dto.Password, dto.ConfirmPassword);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return UserDto.From(user);
    }

    public async Task DeleteAsync(int id)
    {
        if (currentUser.Id == id)
            throw new AppException("Você não pode remover o seu próprio usuário.");

        var user = await FindAsync(id);
        if (user.Role == UserRoles.Admin && await db.Users.CountAsync(u => u.Role == UserRoles.Admin) == 1)
            throw new AppException("Não é possível remover o último administrador.");

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }

    private async Task<User> FindAsync(int id) =>
        await db.Users.FindAsync(id) ?? throw new NotFoundException("Usuário não encontrado.");

    private async Task EnsureLoginIsFreeAsync(string login, int? ignoreId = null)
    {
        if (await db.Users.AnyAsync(u => u.Login == login && u.Id != ignoreId))
            throw new ConflictException($"O login '{login}' já está em uso.");
    }

    private static void ValidatePassword(string? password, string? confirmPassword)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            throw new AppException($"A senha deve ter pelo menos {MinPasswordLength} caracteres.");
        if (confirmPassword is not null && confirmPassword != password)
            throw new AppException("As senhas devem ser iguais.");
    }

    /// <summary>Somente técnicos possuem tarefa; os demais perfis ficam com "N/A".</summary>
    private static string NormalizeTask(string role, string? task)
    {
        if (role != UserRoles.Technician) return "N/A";
        if (string.IsNullOrWhiteSpace(task) || task == "N/A")
            throw new AppException("Informe a tarefa do técnico.");
        return task.Trim();
    }
}
