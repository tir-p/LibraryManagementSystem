using LibraryManagementSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace LibraryManagementSystem.API.Middleware;

// One place for error mapping, so controllers stay free of try/catch:
// missing entity -> 404, broken business rule -> 400, anything else -> 500.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found."),
            DomainException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled error.");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = title }, cancellationToken);
        return true;
    }
}
