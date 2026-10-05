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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException { SqlState: "23505" or "23503" or "23514" })
        {
            await WriteProblemAsync(context, 409, "data_conflict", "The change conflicts with existing records or quantity rules. Refresh and review dependent records.");
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
