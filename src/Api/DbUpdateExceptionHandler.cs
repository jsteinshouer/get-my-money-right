using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Api;

/// <summary>
/// Maps DbUpdateException/DbUpdateConcurrencyException to 409 Conflict directly, ahead of
/// ForEvolve.ExceptionMapper's own pipeline. Its default ProblemDetails serializer reflects over
/// every public property of the caught exception, including DbUpdateException.Entries, whose
/// EntityEntry objects reference the DbContext back and blow up JSON serialization with a cycle
/// error mid-response.
/// </summary>
public class DbUpdateExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public DbUpdateExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The request conflicts with the current state of the server.",
            },
        });
    }
}
