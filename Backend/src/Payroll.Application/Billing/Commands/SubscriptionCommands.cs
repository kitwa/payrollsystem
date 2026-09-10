using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Billing.DTOs;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Audit;
using Payroll.Domain.Billing;
using Payroll.Shared;

namespace Payroll.Application.Billing.Commands;

public record UpgradeSubscriptionCommand(UpgradeSubscriptionDto Dto) : IRequest<Result<SubscriptionSummaryDto>>;

public class UpgradeSubscriptionHandler(
    IAppDbContext db,
    ISubscriptionService subscriptionService,
    IPaymentGateway paymentGateway,
    ICurrentUser currentUser) : IRequestHandler<UpgradeSubscriptionCommand, Result<SubscriptionSummaryDto>>
{
    public async Task<Result<SubscriptionSummaryDto>> Handle(UpgradeSubscriptionCommand request, CancellationToken ct)
    {
        if (currentUser.CompanyId is null)
            return Result<SubscriptionSummaryDto>.Fail("Your account is not linked to a company.");

        var companyId = currentUser.CompanyId.Value;
        var plan = PlanCatalog.GetByCode(request.Dto.PlanCode);
        if (plan is null || plan.Code == PlanCatalog.FreeTrialCode)
            return Result<SubscriptionSummaryDto>.Fail("The selected plan is not valid.");
        if (plan.IsContactSales)
            return Result<SubscriptionSummaryDto>.Fail("Please contact us to configure a plan for more than 500 employees.");

        var employeeCount = await db.Employees.CountAsync(e => e.CompanyId == companyId && !e.IsDeleted, ct);
        if (plan.MaxEmployees is not null && employeeCount > plan.MaxEmployees)
            return Result<SubscriptionSummaryDto>.Fail($"This plan supports up to {plan.MaxEmployees} employees. Reduce your employee count or choose a higher plan.");

        var subscription = await subscriptionService.GetOrCreateAsync(companyId, ct);
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null) return Result<SubscriptionSummaryDto>.Fail("Company not found.");

        if (string.IsNullOrWhiteSpace(subscription.PaymentCustomerReference))
        {
            var customer = await paymentGateway.CreateCustomerAsync(companyId, company.Name, company.Email ?? currentUser.Email, ct);
            subscription.PaymentCustomerReference = customer.CustomerReference;
        }

        // NOTE: no real payment provider is configured yet (see MockPaymentGateway). The mock confirms
        // the checkout synchronously so this server-side call — not the browser — authorises the plan
        // change below. Once a real provider is wired up, redirect to checkout.CheckoutUrl instead and
        // let the provider's webhook (PaymentWebhookController) activate the subscription.
        var checkout = await paymentGateway.CreateCheckoutSessionAsync(
            subscription.PaymentCustomerReference!, plan.Code, plan.MonthlyPrice ?? 0, ct);

        if (!checkout.CompletedImmediately)
            return Result<SubscriptionSummaryDto>.Ok(BuildSummary(subscription, plan, employeeCount));

        subscription.PlanCode = plan.Code;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Price = plan.MonthlyPrice ?? 0;
        subscription.PaymentProvider = paymentGateway.ProviderName;
        subscription.PaymentSubscriptionReference = checkout.SubscriptionReference;
        subscription.CurrentPeriodStart = DateTime.UtcNow;
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
        subscription.ModifiedAt = DateTime.UtcNow;
        subscription.ModifiedBy = currentUser.UserId.ToString();

        db.AuditLogs.Add(new AuditLog
        {
            CompanyId = companyId,
            UserId = currentUser.UserId,
            UserEmail = currentUser.Email,
            HttpMethod = "SYSTEM",
            Path = $"/subscription/upgrade/{plan.Code}",
            StatusCode = 200,
            Action = "SubscriptionPlanChanged",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Result<SubscriptionSummaryDto>.Ok(BuildSummary(subscription, plan, employeeCount));
    }

    private static SubscriptionSummaryDto BuildSummary(CompanySubscription subscription, PlanDefinition plan, int employeeCount) =>
        new(subscription.CompanyId, subscription.Status.ToString(), plan.Code, plan.Name, plan.MinEmployees, plan.MaxEmployees,
            plan.MonthlyPrice, plan.IsContactSales, employeeCount, subscription.TrialStartDate, subscription.TrialEndDate,
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd,
            plan.MaxEmployees is null || employeeCount < plan.MaxEmployees, null);
}

public record CancelSubscriptionCommand : IRequest<Result>;

public class CancelSubscriptionHandler(IAppDbContext db, ISubscriptionService subscriptionService, ICurrentUser currentUser)
    : IRequestHandler<CancelSubscriptionCommand, Result>
{
    public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken ct)
    {
        if (currentUser.CompanyId is null) return Result.Fail("Your account is not linked to a company.");

        var subscription = await subscriptionService.GetOrCreateAsync(currentUser.CompanyId.Value, ct);
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            UserId = currentUser.UserId,
            UserEmail = currentUser.Email,
            HttpMethod = "SYSTEM",
            Path = "/subscription/cancel",
            StatusCode = 200,
            Action = "SubscriptionCancelled",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
