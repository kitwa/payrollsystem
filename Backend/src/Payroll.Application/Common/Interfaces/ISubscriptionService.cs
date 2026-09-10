using Payroll.Domain.Billing;

namespace Payroll.Application.Common.Interfaces;

public record EmployeeLimitCheck(bool Allowed, string? Message, string? RecommendedPlanCode);

/// <summary>
/// The single authority for subscription status, plan limits, and employee-count enforcement.
/// Employee creation and payroll processing must always go through this service — never duplicate
/// plan/limit logic in a controller or handler.
/// </summary>
public interface ISubscriptionService
{
    Task<CompanySubscription> GetOrCreateAsync(Guid companyId, CancellationToken ct = default);
    Task<EmployeeLimitCheck> CheckCanAddEmployeeAsync(Guid companyId, CancellationToken ct = default);
    Task<bool> IsUsableAsync(Guid companyId, CancellationToken ct = default);
    Task<int> ExpireOverdueTrialsAsync(CancellationToken ct = default);
}
