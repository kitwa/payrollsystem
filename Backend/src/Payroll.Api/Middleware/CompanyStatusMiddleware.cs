using Microsoft.EntityFrameworkCore;
using Payroll.Infrastructure.Persistence;
using Payroll.Shared;

namespace Payroll.Api.Middleware;

/// <summary>
/// Blocks all normal API access for users whose company has been disabled by a Super Admin.
/// This is enforced here (not just hidden in Angular) so a disabled company cannot bypass the
/// restriction via direct API calls. Auth, subscription/billing, and support endpoints stay reachable
/// so the company can still contact support or view billing status.
/// </summary>
public class CompanyStatusMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedWhenDisabled = ["/api/auth", "/api/subscription", "/api/support"];

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.User.IsInRole(Constants.Roles.SuperAdmin)
            && !AllowedWhenDisabled.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            var companyIdClaim = context.User.FindFirst(Constants.ClaimTypes.CompanyId)?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                var isActive = await db.Companies
                    .Where(c => c.Id == companyId)
                    .Select(c => (bool?)c.IsActive)
                    .FirstOrDefaultAsync();

                if (isActive == false)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { errors = new[] { "Your company account has been disabled. Please contact support." } });
                    return;
                }
            }
        }

        await next(context);
    }
}
