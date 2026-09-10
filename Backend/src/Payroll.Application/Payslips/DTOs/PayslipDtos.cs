using Payroll.Domain.PayrollRuns.Enums;

namespace Payroll.Application.Payslips.DTOs;

public record PayslipListItemDto(
    Guid PayrollLineId,
    Guid PayrollPeriodId,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeNumber,
    string? Email,
    int Year,
    int Month,
    PayrollStatus Status,
    decimal GrossEarnings,
    decimal TaxableIncome,
    decimal TotalDeductions,
    decimal NetPay,
    DateTime? EmailedAt);

public record PayslipPeriodSummaryDto(
    Guid PayrollPeriodId,
    int Year,
    int Month,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    PayrollStatus Status,
    int PayslipCount,
    int EmailedCount,
    decimal TotalNet);

public record PayslipLineItemDto(string Category, string Description, decimal Amount);

public record PayslipDetailDto(
    Guid PayrollLineId,
    Guid PayrollPeriodId,
    string CompanyName,
    string EmployeeName,
    string EmployeeNumber,
    string? Email,
    string? JobTitle,
    string? Department,
    string? TaxNumber,
    string? UifNumber,
    int Year,
    int Month,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    PayrollStatus Status,
    decimal GrossEarnings,
    decimal TaxableIncome,
    decimal TotalDeductions,
    decimal NetPay,
    DateTime? EmailedAt,
    List<PayslipLineItemDto> Earnings,
    List<PayslipLineItemDto> Deductions);

public record EmailPayslipResultDto(int Sent, int Skipped, List<string> Failures);
