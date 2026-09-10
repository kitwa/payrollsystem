using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Billing;

namespace Payroll.Application.Billing;

public class SubscriptionService(IAppDbContext db) : ISubscriptionService
{
    public async Task<CompanySubscription> GetOrCreateAsync(Guid companyId, CancellationToken ct = default)
    {
        var subscription = await db.CompanySubscriptions.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        if (subscription is null)
        {
            var now = DateTime.UtcNow;
            subscription = new CompanySubscription
            {
                CompanyId = companyId,
                PlanCode = PlanCatalog.FreeTrialCode,
                Status = SubscriptionStatus.FreeTrial,
                TrialStartDate = now,
                TrialEndDate = now.AddMonths(1),
                Price = 0
            };
            db.CompanySubscriptions.Add(subscription);
            await db.SaveChangesAsync(ct);
        }

        var effectiveStatus = SubscriptionRules.DetermineEffectiveStatus(subscription.Status, subscription.TrialEndDate, DateTime.UtcNow);
        if (effectiveStatus != subscription.Status)
        {
            subscription.Status = effectiveStatus;
            await db.SaveChangesAsync(ct);
        }

        return subscription;
    }

    public async Task<EmployeeLimitCheck> CheckCanAddEmployeeAsync(Guid companyId, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateAsync(companyId, ct);
        if (!SubscriptionRules.IsUsable(subscription.Status))
            return new EmployeeLimitCheck(false, "Your free month has ended or your subscription is inactive. Choose a plan to continue using Payroll SA.", null);

        var employeeCount = await db.Employees.CountAsync(e => e.CompanyId == companyId && !e.IsDeleted, ct);
        if (SubscriptionRules.CanAddEmployee(subscription.PlanCode, employeeCount))
            return new EmployeeLimitCheck(true, null, null);

        var plan = PlanCatalog.GetByCode(subscription.PlanCode);
        var recommended = PlanCatalog.Recommend(employeeCount + 1);
        return new EmployeeLimitCheck(
            false,
            $"Your current plan supports a maximum of {plan?.MaxEmployees} employees. Upgrade your plan to add more employees.",
            recommended.Code);
    }

    public async Task<bool> IsUsableAsync(Guid companyId, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateAsync(companyId, ct);
        return SubscriptionRules.IsUsable(subscription.Status);
    }

    public async Task<int> ExpireOverdueTrialsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var overdue = await db.CompanySubscriptions
            .Where(s => s.Status == SubscriptionStatus.FreeTrial && s.TrialEndDate != null && s.TrialEndDate < now)
            .ToListAsync(ct);

        foreach (var subscription in overdue)
            subscription.Status = SubscriptionStatus.Expired;

        if (overdue.Count > 0) await db.SaveChangesAsync(ct);
        return overdue.Count;
    }
}
