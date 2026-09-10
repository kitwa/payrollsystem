namespace Payroll.Application.Billing.DTOs;

public record PlanDto(string Code, string Name, int MinEmployees, int? MaxEmployees, decimal? MonthlyPrice, bool IsContactSales);

public record SubscriptionSummaryDto(
    Guid CompanyId,
    string Status,
    string PlanCode,
    string PlanName,
    int MinEmployees,
    int? MaxEmployees,
    decimal? MonthlyPrice,
    bool IsContactSales,
    int CurrentEmployeeCount,
    DateTime? TrialStartDate,
    DateTime? TrialEndDate,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    bool CanAddEmployee,
    string? RecommendedPlanCode);

public record UpgradeSubscriptionDto(string PlanCode);

public record WebhookEventDto(
    string EventId,
    string EventType,
    Guid CompanyId,
    string? PlanCode,
    decimal? Amount,
    string? PaymentCustomerReference,
    string? PaymentSubscriptionReference);
