namespace Payroll.Domain.Billing;

/// <summary>
/// Single source of truth for pricing tiers and employee limits. Every part of the application
/// (employee-limit enforcement, subscription summaries, upgrade flow, Angular pricing page) reads
/// from this catalog instead of duplicating prices/limits.
/// </summary>
public static class PlanCatalog
{
    public const string FreeTrialCode = "FREE_TRIAL";

    public static readonly IReadOnlyList<PlanDefinition> Plans =
    [
        new(FreeTrialCode, "Free Trial", 0, 5, 0m, false),
        new("SMALL_BUSINESS", "Small Business", 1, 5, 49m, false),
        new("GROWING_BUSINESS", "Growing Business", 6, 10, 89m, false),
        new("GROWING_TEAM", "Growing Team", 11, 20, 169m, false),
        new("BUSINESS", "Business", 21, 30, 259m, false),
        new("BUSINESS_PLUS_40", "Business Plus", 31, 40, 349m, false),
        new("BUSINESS_PLUS_50", "Business Plus", 41, 50, 449m, false),
        new("GROWING_COMPANY_75", "Growing Company", 51, 75, 599m, false),
        new("GROWING_COMPANY_100", "Growing Company", 76, 100, 799m, false),
        new("LARGE_BUSINESS_150", "Large Business", 101, 150, 999m, false),
        new("LARGE_BUSINESS_200", "Large Business", 151, 200, 1299m, false),
        new("ENTERPRISE_300", "Enterprise", 201, 300, 1599m, false),
        new("ENTERPRISE_500", "Enterprise", 301, 500, 1999m, false),
        new("ENTERPRISE_CONTACT", "Enterprise", 501, null, null, true)
    ];

    public static PlanDefinition? GetByCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : Plans.FirstOrDefault(p => p.Code == code);

    /// <summary>The cheapest paid plan (excludes the free trial) that supports the given employee count.</summary>
    public static PlanDefinition Recommend(int employeeCount) =>
        Plans
            .Where(p => p.Code != FreeTrialCode)
            .OrderBy(p => p.MinEmployees)
            .FirstOrDefault(p => employeeCount <= (p.MaxEmployees ?? int.MaxValue))
        ?? Plans[^1];
}
