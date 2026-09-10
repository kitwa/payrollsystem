using Payroll.Domain.Common;

namespace Payroll.Domain.Billing;

/// <summary>Records processed webhook events so duplicate provider notifications are ignored (idempotency).</summary>
public class PaymentWebhookEvent : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
