namespace Payroll.Application.Dashboard.DTOs;

public record DashboardSummaryDto(
    int TotalEmployees,
    int ActiveEmployees,
    int OnLeaveEmployees,
    int PendingLeaveRequests,
    Guid? CurrentPeriodId,
    string? CurrentPeriodStatus,
    int CurrentPeriodEmployeeCount,
    decimal CurrentPeriodTotalNet,
    decimal YtdGross,
    decimal YtdDeductions,
    decimal YtdNet);
