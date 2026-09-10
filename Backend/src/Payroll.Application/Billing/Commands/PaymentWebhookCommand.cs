using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Billing.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Audit;
using Payroll.Domain.Billing;
using Payroll.Shared;

namespace Payroll.Application.Billing.Commands;

public record ProcessPaymentWebhookCommand(WebhookEventDto Dto) : IRequest<Result>;

/// <summary>
/// Handles trusted payment-provider events (never trust the Angular app for payment status).
/// Idempotent — replaying the same EventId is a safe no-op.
/// </summary>
public class ProcessPaymentWebhookHandler(IAppDbContext db, ISubscriptionService subscriptionService)
    : IRequestHandler<ProcessPaymentWebhookCommand, Result>
{
    public async Task<Result> Handle(ProcessPaymentWebhookCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        if (string.IsNullOrWhiteSpace(dto.EventId)) return Result.Fail("A webhook event id is required.");

        var alreadyProcessed = await db.PaymentWebhookEvents.AnyAsync(e => e.EventId == dto.EventId, ct);
        if (alreadyProcessed) return Result.Ok();

        var subscription = await subscriptionService.GetOrCreateAsync(dto.CompanyId, ct);

        switch (dto.EventType)
        {
            case "payment_succeeded":
            case "subscription_activated":
            case "subscription_renewed":
                if (!string.IsNullOrWhiteSpace(dto.PlanCode))
                {
                    var plan = PlanCatalog.GetByCode(dto.PlanCode);
                    if (plan is not null)
                    {
                        subscription.PlanCode = plan.Code;
                        subscription.Price = plan.MonthlyPrice ?? subscription.Price;
                    }
                }
                subscription.Status = SubscriptionStatus.Active;
                subscription.CurrentPeriodStart = DateTime.UtcNow;
                subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
                subscription.PaymentSubscriptionReference = dto.PaymentSubscriptionReference ?? subscription.PaymentSubscriptionReference;
                subscription.PaymentCustomerReference = dto.PaymentCustomerReference ?? subscription.PaymentCustomerReference;
                break;
            case "payment_failed":
                subscription.Status = SubscriptionStatus.PastDue;
                break;
            case "subscription_cancelled":
            case "subscription_expired":
                subscription.Status = dto.EventType == "subscription_expired" ? SubscriptionStatus.Expired : SubscriptionStatus.Cancelled;
                subscription.CancelledAt = DateTime.UtcNow;
                break;
            default:
                return Result.Fail($"Unsupported webhook event type: {dto.EventType}");
        }

        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { EventId = dto.EventId, EventType = dto.EventType, CompanyId = dto.CompanyId });
        db.AuditLogs.Add(new AuditLog
        {
            CompanyId = dto.CompanyId,
            HttpMethod = "SYSTEM",
            Path = "/payment/webhook",
            StatusCode = 200,
            Action = $"PaymentWebhook:{dto.EventType}",
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
