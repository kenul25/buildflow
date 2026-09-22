using System.Text.Json;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            await WriteProblemAsync(context, exception.StatusCode, exception.Code, exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request failure");
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "server_error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string code, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = code,
            Detail = detail,
            Instance = context.Request.Path
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
