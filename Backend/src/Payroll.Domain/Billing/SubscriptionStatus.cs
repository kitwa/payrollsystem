namespace Payroll.Domain.Billing;

public enum SubscriptionStatus
{
    FreeTrial,
    Active,
    PastDue,
    Cancelled,
    Expired,
    Suspended
}
