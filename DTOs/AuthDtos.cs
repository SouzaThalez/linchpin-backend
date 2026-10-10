namespace LinchpinBackend.DTOs;

public record LoginRequest(string Login, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);
