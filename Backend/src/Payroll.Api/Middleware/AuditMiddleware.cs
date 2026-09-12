using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Audit;
using Payroll.Infrastructure.Persistence;
using Payroll.Shared;

namespace Payroll.Api.Middleware;

public class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger, IServiceScopeFactory scopeFactory)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            await next(context);
        }
        finally
        {
            if (ShouldAudit(context))
            {
                using var auditScope = scopeFactory.CreateScope();
                var db = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var claimCompanyId = Guid.TryParse(
                    context.User.FindFirstValue(Constants.ClaimTypes.CompanyId), out var parsedCompanyId)
                    ? parsedCompanyId
                    : (Guid?)null;
                var companyId = ResolveCompanyId(context, claimCompanyId);
                var historyEnabled = companyId is null || await IsActivityHistoryEnabledAsync(db, companyId.Value, context.RequestAborted);
                if (historyEnabled)
                {
                    var userId = Guid.TryParse(
                        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub"), out var parsedUserId)
                        ? parsedUserId
                        : (Guid?)null;

                    db.AuditLogs.Add(new AuditLog
                    {
                        CompanyId = companyId,
                        UserId = userId,
                        UserEmail = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email"),
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        HttpMethod = context.Request.Method,
                        Path = context.Request.Path,
                        StatusCode = context.Response.StatusCode,
                        Action = $"{context.Request.Method} {context.Request.Path}",
                        OccurredAt = startedAt
                    });

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Could not persist audit log for {Path}", context.Request.Path);
                    }
                }
            }
        }
    }

    private static async Task<bool> IsActivityHistoryEnabledAsync(AppDbContext db, Guid companyId, CancellationToken ct)
    {
        return await db.Companies.Where(c => c.Id == companyId && !c.IsDeleted)
            .Select(c => (bool?)c.IsActivityHistoryEnabled)
            .FirstOrDefaultAsync(ct)
            ?? true;
    }

    private static bool ShouldAudit(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api")
        && !context.Request.Path.StartsWithSegments("/api/audit")
        && !context.Request.Path.StartsWithSegments("/api/management");

    /// <summary>Falls back to the "companyId" route/query value — needed for SuperAdmin, who has no company claim.</summary>
    private static Guid? ResolveCompanyId(HttpContext context, Guid? claimCompanyId)
    {
        if (claimCompanyId is not null) return claimCompanyId;

        if (context.Request.RouteValues.TryGetValue("companyId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeCompanyId))
            return routeCompanyId;

        if (context.Request.Query.TryGetValue("companyId", out var queryValues)
            && Guid.TryParse(queryValues.FirstOrDefault(), out var queryCompanyId))
            return queryCompanyId;

        // CompaniesController routes address the company directly via "{id}".
        if (context.Request.Path.StartsWithSegments("/api/companies")
            && context.Request.RouteValues.TryGetValue("id", out var idValue)
            && Guid.TryParse(idValue?.ToString(), out var companyRouteId))
            return companyRouteId;

        return null;
    }
}
