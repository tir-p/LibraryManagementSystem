// ---------------------------------------------------------------------------
// File: GlobalExceptionHandler.cs
// Purpose: Central API error mapping — converts exceptions to JSON HTTP responses
//   so controllers stay free of try/catch. Registered in Program.cs.
// ---------------------------------------------------------------------------
using LibraryManagementSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace LibraryManagementSystem.API.Middleware;

// One place for error mapping, so controllers stay free of try/catch:
// missing entity -> 404, broken business rule -> 400, anything else -> 500.
/// <summary>
/// Central exception-to-HTTP mapper (implements <see cref="IExceptionHandler"/>).
/// Mapping: <c>KeyNotFoundException</c> -&gt; 404, <c>DomainException</c> -&gt; 400, else 500.
/// DI: <c>ILogger</c> injected for 500s only; registered via AddExceptionHandler in Program.cs.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Creates the handler. DI container supplies the typed logger.
    /// </summary>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Maps an unhandled exception to a JSON HTTP response.
    /// Returns true when handled (always true here — pipeline stops).
    /// Status mapping: KeyNotFoundException -&gt; 404, DomainException -&gt; 400, else 500.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Junior note: switch expression picks (status, message) by exception type.
        // KeyNotFound = service could not find entity; DomainException = business rule.
        var (status, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found."),
            DomainException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            // Log 500s only: 404/400 are expected client errors, not bugs.
            _logger.LogError(exception, "Unhandled error.");

        // Write uniform JSON shape { message } so frontend can show title directly.
        // Write uniform JSON shape { message } so frontend can show title directly.
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = title }, cancellationToken);
        // true = "we handled it", stops further exception propagation.
        return true;
    }
}
