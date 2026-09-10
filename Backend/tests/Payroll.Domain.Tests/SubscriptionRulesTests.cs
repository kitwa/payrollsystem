using FluentAssertions;

namespace Payroll.Domain.Tests;

public class SubscriptionRulesTests
{
    [Theory]
    [InlineData("SMALL_BUSINESS", 4, true)]
    [InlineData("SMALL_BUSINESS", 5, false)]
    [InlineData("SMALL_BUSINESS", 6, false)]
    [InlineData("FREE_TRIAL", 4, true)]
    [InlineData("FREE_TRIAL", 5, false)]
    [InlineData("ENTERPRISE_CONTACT", 10000, true)]
    [InlineData("UNKNOWN_CODE", 1, true)]
    public void CanAddEmployee_respects_plan_max_employees(string planCode, int currentEmployeeCount, bool expected)
    {
        Payroll.Domain.Billing.SubscriptionRules.CanAddEmployee(planCode, currentEmployeeCount)
            .Should().Be(expected);
    }

    [Fact]
    public void IsTrialExpired_is_false_when_no_end_date_set()
    {
        Payroll.Domain.Billing.SubscriptionRules.IsTrialExpired(null, DateTime.UtcNow)
            .Should().BeFalse();
    }

    [Fact]
    public void IsTrialExpired_is_true_once_utcNow_passes_the_end_date()
    {
        var trialEnd = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Payroll.Domain.Billing.SubscriptionRules.IsTrialExpired(trialEnd, trialEnd.AddSeconds(1))
            .Should().BeTrue();
        Payroll.Domain.Billing.SubscriptionRules.IsTrialExpired(trialEnd, trialEnd.AddSeconds(-1))
            .Should().BeFalse();
    }

    [Fact]
    public void DetermineEffectiveStatus_expires_an_overdue_free_trial()
    {
        var trialEnd = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Payroll.Domain.Billing.SubscriptionRules
            .DetermineEffectiveStatus(Payroll.Domain.Billing.SubscriptionStatus.FreeTrial, trialEnd, trialEnd.AddDays(1))
            .Should().Be(Payroll.Domain.Billing.SubscriptionStatus.Expired);
    }

    [Fact]
    public void DetermineEffectiveStatus_leaves_an_active_trial_untouched()
    {
        var trialEnd = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Payroll.Domain.Billing.SubscriptionRules
            .DetermineEffectiveStatus(Payroll.Domain.Billing.SubscriptionStatus.FreeTrial, trialEnd, trialEnd.AddDays(-1))
            .Should().Be(Payroll.Domain.Billing.SubscriptionStatus.FreeTrial);
    }

    [Fact]
    public void DetermineEffectiveStatus_never_changes_a_non_trial_status()
    {
        var trialEnd = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Payroll.Domain.Billing.SubscriptionRules
            .DetermineEffectiveStatus(Payroll.Domain.Billing.SubscriptionStatus.Cancelled, trialEnd, trialEnd.AddYears(1))
            .Should().Be(Payroll.Domain.Billing.SubscriptionStatus.Cancelled);
    }

    [Theory]
    [InlineData(Payroll.Domain.Billing.SubscriptionStatus.FreeTrial, true)]
    [InlineData(Payroll.Domain.Billing.SubscriptionStatus.Active, true)]
    [InlineData(Payroll.Domain.Billing.SubscriptionStatus.PastDue, false)]
    [InlineData(Payroll.Domain.Billing.SubscriptionStatus.Cancelled, false)]
    [InlineData(Payroll.Domain.Billing.SubscriptionStatus.Expired, false)]
    [InlineData(Payroll.Domain.Billing.SubscriptionStatus.Suspended, false)]
    public void IsUsable_is_true_only_for_free_trial_and_active(Payroll.Domain.Billing.SubscriptionStatus status, bool expected)
    {
        Payroll.Domain.Billing.SubscriptionRules.IsUsable(status).Should().Be(expected);
    }
}
