namespace Payroll.Domain.Billing;

/// <summary>One pricing tier. Immutable — plans are looked up, never edited in place.</summary>
public record PlanDefinition(
    string Code,
    string Name,
    int MinEmployees,
    int? MaxEmployees,
    decimal? MonthlyPrice,
    bool IsContactSales);
