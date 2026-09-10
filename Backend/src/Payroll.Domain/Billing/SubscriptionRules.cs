namespace Payroll.Domain.Billing;

/// <summary>Pure business rules for subscription/plan enforcement — no database access, fully unit-testable.</summary>
public static class SubscriptionRules
{
    public static bool CanAddEmployee(string? planCode, int currentEmployeeCount)
    {
        var plan = PlanCatalog.GetByCode(planCode);
        if (plan?.MaxEmployees is null) return true;
        return currentEmployeeCount < plan.MaxEmployees;
    }

    public static bool IsTrialExpired(DateTime? trialEndDate, DateTime utcNow) =>
        trialEndDate is not null && utcNow > trialEndDate;

    /// <summary>Recomputes status server-side — a free trial expires purely based on the stored end date.</summary>
    public static SubscriptionStatus DetermineEffectiveStatus(SubscriptionStatus currentStatus, DateTime? trialEndDate, DateTime utcNow) =>
        currentStatus == SubscriptionStatus.FreeTrial && IsTrialExpired(trialEndDate, utcNow)
            ? SubscriptionStatus.Expired
            : currentStatus;

    public static bool IsUsable(SubscriptionStatus status) =>
        status is SubscriptionStatus.FreeTrial or SubscriptionStatus.Active;
}
