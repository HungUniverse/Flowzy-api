using Flowzy.Service.Contracts;

namespace Flowzy.Api.Middleware;

public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.Ordinal)
    {
        "/api/auth/me",
        "/api/auth/logout",
        "/api/profile/me/password"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            string.Equals(context.User.FindFirst("mustChangePassword")?.Value, "true", StringComparison.OrdinalIgnoreCase) &&
            !AllowedPaths.Contains(context.Request.Path.Value ?? string.Empty))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Error(403, "Password change is required before using this feature"));
            return;
        }
        await next(context);
    }
}
