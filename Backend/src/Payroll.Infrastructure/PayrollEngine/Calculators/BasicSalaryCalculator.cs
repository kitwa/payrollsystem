using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Infrastructure.PayrollEngine.Interfaces;

namespace Payroll.Infrastructure.PayrollEngine.Calculators;

/// <summary>Adds basic salary as a taxable earning line.</summary>
public class BasicSalaryCalculator : IPayrollCalculator
{
    public int Order => 10;

    public Task CalculateAsync(PayrollContext context, CancellationToken ct = default)
    {
        context.Earnings.Add(new Earning
        {
            PayrollLineId = context.PeriodId, // will be set by engine after line is saved
            Category = EarningCategory.BasicSalary,
            Description = "Basic Salary",
            Amount = context.BasicSalary,
            IsTaxable = true
        });

        context.TaxableIncome += context.BasicSalary;
        return Task.CompletedTask;
    }
}
