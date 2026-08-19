using Payroll.Domain.PayrollRuns.Enums;

namespace Payroll.Application.Payslips.DTOs;

public record PayslipListItemDto(
    Guid PayrollLineId,
    Guid PayrollPeriodId,
    int Year,
    int Month,
    PayrollStatus Status,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal NetPay);
