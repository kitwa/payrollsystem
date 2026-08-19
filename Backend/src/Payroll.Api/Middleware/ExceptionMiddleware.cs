using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Diagnostics;
using Payroll.Shared;

namespace Payroll.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { errors = new[] { "An unexpected error occurred." } });
        }
    }
}

/// <summary>Restricts Hangfire dashboard to Admin role.</summary>
public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.IsInRole(Constants.Roles.Admin) || http.User.IsInRole(Constants.Roles.SuperAdmin);
    }
}
