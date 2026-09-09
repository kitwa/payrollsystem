using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Infrastructure.PayrollEngine.Interfaces;

namespace Payroll.Infrastructure.PayrollEngine.Calculators;

/// <summary>Deducts a pro-rata daily rate for approved unpaid leave days that fall within the payroll period.</summary>
public class UnpaidLeaveCalculator : IPayrollCalculator
{
    public int Order => 20;

    public Task CalculateAsync(PayrollContext context, CancellationToken ct = default)
    {
        if (context.UnpaidLeaveDays <= 0) return Task.CompletedTask;

        var daysInPeriod = (context.PeriodEnd - context.PeriodStart).Days + 1;
        if (daysInPeriod <= 0) return Task.CompletedTask;

        var dailyRate = context.BasicSalary / daysInPeriod;
        var deductionAmount = Math.Round(dailyRate * context.UnpaidLeaveDays, 2);
        if (deductionAmount <= 0) return Task.CompletedTask;

        context.Deductions.Add(new Deduction
        {
            Category = DeductionCategory.UnpaidLeave,
            Description = $"Unpaid Leave ({context.UnpaidLeaveDays:0.##} day(s))",
            EmployeeAmount = deductionAmount,
            EmployerAmount = 0
        });

        context.TaxableIncome -= deductionAmount;
        if (context.TaxableIncome < 0) context.TaxableIncome = 0;

        return Task.CompletedTask;
    }
}
