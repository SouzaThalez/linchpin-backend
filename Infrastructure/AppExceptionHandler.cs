using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LinchpinBackend.Infrastructure;

/// <summary>Converte exceções em respostas ProblemDetails ({ title, status, detail }).</summary>
public class AppExceptionHandler(ILogger<AppExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            AppException app => (app.StatusCode, app.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno no servidor.")
        };

        if (status >= 500)
            logger.LogError(exception, "Erro não tratado em {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
