using Payroll.Domain.PayrollRuns.Enums;

namespace Payroll.Application.Payroll.DTOs;

public record PayrollPeriodDto(
    Guid Id,
    Guid CompanyId,
    int Year,
    int Month,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    PayrollStatus Status,
    int EmployeeCount,
    decimal TotalGross,
    decimal TotalDeductions,
    decimal TotalNet);

public record PayrollLineDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeNumber,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal NetPay,
    List<EarningDto> Earnings,
    List<DeductionDto> Deductions);

public record EarningDto(EarningCategory Category, string Description, decimal Amount);
public record DeductionDto(DeductionCategory Category, string Description, decimal EmployeeAmount, decimal EmployerAmount);

public record GeneratePayrollDto(Guid CompanyId, int Year, int Month);
