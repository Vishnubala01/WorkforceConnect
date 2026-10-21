using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;

namespace WorkforceConnect.Api.Middleware;

/// <summary>Turns application exceptions into RFC 7807 problem responses. Unexpected errors never leak details.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Message),
            AppValidationException e => (StatusCodes.Status400BadRequest, "Validation failed", e.Message),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Forbidden", e.Message),
            InvalidCredentialsException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Message),
            UnauthenticatedException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflict", "The change conflicts with existing data."),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else if (exception is DbUpdateException)
        {
            logger.LogWarning(exception, "Database conflict for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json");
    }
}
