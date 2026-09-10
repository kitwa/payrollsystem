using Payroll.Domain.Common;

namespace Payroll.Domain.Billing;

public class CompanySubscription : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string PlanCode { get; set; } = PlanCatalog.FreeTrialCode;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.FreeTrial;
    public DateTime? TrialStartDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string? PaymentProvider { get; set; }
    public string? PaymentCustomerReference { get; set; }
    public string? PaymentSubscriptionReference { get; set; }
    public string? PaymentMethodReference { get; set; }
    public DateTime? CancelledAt { get; set; }
}
