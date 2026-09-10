using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Audit;
using Payroll.Domain.Billing;
using Payroll.Shared;

namespace Payroll.Application.Admin;

public record AdminCompanyDto(
    Guid Id, string Name, bool IsActive, string SubscriptionStatus, string PlanCode, string PlanName,
    int EmployeeCount, int? MaxEmployees, DateTime? TrialEndDate, DateTime? CurrentPeriodEnd, DateTime CreatedAt);

public record GetAdminCompaniesQuery : IRequest<Result<List<AdminCompanyDto>>>;

public class GetAdminCompaniesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetAdminCompaniesQuery, Result<List<AdminCompanyDto>>>
{
    public async Task<Result<List<AdminCompanyDto>>> Handle(GetAdminCompaniesQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result<List<AdminCompanyDto>>.Fail("Only Super Admin users can view company administration.");

        var companies = await db.Companies.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(ct);
        var companyIds = companies.Select(c => c.Id).ToList();
        var subscriptions = await db.CompanySubscriptions.Where(s => companyIds.Contains(s.CompanyId)).ToListAsync(ct);
        var employeeCounts = await db.Employees
            .Where(e => !e.IsDeleted && companyIds.Contains(e.CompanyId))
            .GroupBy(e => e.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var result = companies.Select(company =>
        {
            var subscription = subscriptions.FirstOrDefault(s => s.CompanyId == company.Id);
            var status = subscription is null
                ? SubscriptionStatus.FreeTrial
                : SubscriptionRules.DetermineEffectiveStatus(subscription.Status, subscription.TrialEndDate, DateTime.UtcNow);
            var plan = PlanCatalog.GetByCode(subscription?.PlanCode) ?? PlanCatalog.Plans[0];
            var employeeCount = employeeCounts.FirstOrDefault(e => e.CompanyId == company.Id)?.Count ?? 0;

            return new AdminCompanyDto(
                company.Id, company.Name, company.IsActive, status.ToString(), plan.Code, plan.Name,
                employeeCount, plan.MaxEmployees, subscription?.TrialEndDate, subscription?.CurrentPeriodEnd, company.CreatedAt);
        }).ToList();

        return Result<List<AdminCompanyDto>>.Ok(result);
    }
}

public record DisableCompanyCommand(Guid CompanyId) : IRequest<Result>;

public class DisableCompanyHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<DisableCompanyCommand, Result>
{
    public async Task<Result> Handle(DisableCompanyCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result.Fail("Only Super Admin users can disable a company.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId && !c.IsDeleted, ct);
        if (company is null) return Result.Fail("Company not found.");

        company.IsActive = false;
        db.AuditLogs.Add(new AuditLog
        {
            CompanyId = company.Id,
            UserId = currentUser.UserId,
            UserEmail = currentUser.Email,
            HttpMethod = "SYSTEM",
            Path = $"/admin/companies/{company.Id}/disable",
            StatusCode = 200,
            Action = "CompanyDisabled",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record EnableCompanyCommand(Guid CompanyId) : IRequest<Result>;

public class EnableCompanyHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<EnableCompanyCommand, Result>
{
    public async Task<Result> Handle(EnableCompanyCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result.Fail("Only Super Admin users can enable a company.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId && !c.IsDeleted, ct);
        if (company is null) return Result.Fail("Company not found.");

        company.IsActive = true;
        db.AuditLogs.Add(new AuditLog
        {
            CompanyId = company.Id,
            UserId = currentUser.UserId,
            UserEmail = currentUser.Email,
            HttpMethod = "SYSTEM",
            Path = $"/admin/companies/{company.Id}/enable",
            StatusCode = 200,
            Action = "CompanyEnabled",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
