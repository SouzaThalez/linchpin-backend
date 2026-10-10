namespace LinchpinBackend.Infrastructure;

/// <summary>Erro de regra de negócio, devolvido ao cliente com o status informado.</summary>
public class AppException(string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);

public class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);

public class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);
