namespace Payroll.Application.Management.DTOs;

public record CompanyManagementSummaryDto(
    Guid CompanyId,
    string CompanyName,
    int UserCount,
    int ActiveUserCount,
    int EmployeeCount,
    int ActiveEmployeeCount,
    int PayrollPeriodCount,
    int PayslipCount,
    int EmailedPayslipCount,
    decimal LatestPayrollNet,
    DateTime? LatestPayrollDate,
    DateTime? LastActivityAt);

public record ManagementAuditDto(
    Guid Id,
    Guid? CompanyId,
    string? UserEmail,
    string? IpAddress,
    string HttpMethod,
    string Path,
    int StatusCode,
    string? Action,
    DateTime OccurredAt);
