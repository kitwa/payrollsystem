using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Billing.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Billing;
using Payroll.Shared;

namespace Payroll.Application.Billing.Queries;

public record GetMySubscriptionQuery : IRequest<Result<SubscriptionSummaryDto>>;

public class GetMySubscriptionHandler(IAppDbContext db, ISubscriptionService subscriptionService, ICurrentUser currentUser)
    : IRequestHandler<GetMySubscriptionQuery, Result<SubscriptionSummaryDto>>
{
    public async Task<Result<SubscriptionSummaryDto>> Handle(GetMySubscriptionQuery request, CancellationToken ct)
    {
        if (currentUser.CompanyId is null)
            return Result<SubscriptionSummaryDto>.Fail("Your account is not linked to a company.");

        var companyId = currentUser.CompanyId.Value;
        var subscription = await subscriptionService.GetOrCreateAsync(companyId, ct);
        var employeeCount = await db.Employees.CountAsync(e => e.CompanyId == companyId && !e.IsDeleted, ct);
        var plan = PlanCatalog.GetByCode(subscription.PlanCode) ?? PlanCatalog.Plans[0];
        var canAdd = SubscriptionRules.IsUsable(subscription.Status) && SubscriptionRules.CanAddEmployee(subscription.PlanCode, employeeCount);
        var recommended = canAdd ? null : PlanCatalog.Recommend(employeeCount + 1).Code;

        return Result<SubscriptionSummaryDto>.Ok(new SubscriptionSummaryDto(
            companyId, subscription.Status.ToString(), plan.Code, plan.Name, plan.MinEmployees, plan.MaxEmployees,
            plan.MonthlyPrice, plan.IsContactSales, employeeCount, subscription.TrialStartDate, subscription.TrialEndDate,
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, canAdd, recommended));
    }
}

public record GetPlansQuery : IRequest<Result<List<PlanDto>>>;

public class GetPlansHandler : IRequestHandler<GetPlansQuery, Result<List<PlanDto>>>
{
    public Task<Result<List<PlanDto>>> Handle(GetPlansQuery request, CancellationToken ct) =>
        Task.FromResult(Result<List<PlanDto>>.Ok(PlanCatalog.Plans
            .Where(p => p.Code != PlanCatalog.FreeTrialCode)
            .Select(p => new PlanDto(p.Code, p.Name, p.MinEmployees, p.MaxEmployees, p.MonthlyPrice, p.IsContactSales))
            .ToList()));
}
