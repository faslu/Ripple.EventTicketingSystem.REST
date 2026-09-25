using Ripple.EventTicketingSystem.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace RippleEventTracking.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(
                context,
                ex.Message,
                HttpStatusCode.NotFound);
        }
        catch (ConflictException ex)
        {
            await HandleExceptionAsync(
                context,
                ex.Message,
                HttpStatusCode.Conflict);
        }
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(
                context,
                ex.Message,
                HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception.");

            await HandleExceptionAsync(
                context,
                "An unexpected error occurred.",
                HttpStatusCode.InternalServerError);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        string message,
        HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = (int)statusCode,
            message,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}