using System.Text.Json;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Message);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Optimistic concurrency conflict");
            await WriteErrorAsync(context, 409,
                "The resource has been modified by another transaction. Please reload and try again.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Database integrity violation");
            await WriteErrorAsync(context, 409, "Database integrity violation or conflict");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception caught");
            await WriteErrorAsync(context, 500, "An unexpected error occurred");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Error(status, message),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
