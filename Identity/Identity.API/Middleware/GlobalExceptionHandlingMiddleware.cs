using FluentValidation;
using System.Net;

namespace Identity.API.Middleware;

/// <summary>
/// Converts common application exceptions into standardized HTTP responses.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Executes the middleware logic for the current request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous middleware operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Validation failed", details = exception.Errors.Select(error => error.ErrorMessage) });
        }
        catch (KeyNotFoundException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }
    }
}