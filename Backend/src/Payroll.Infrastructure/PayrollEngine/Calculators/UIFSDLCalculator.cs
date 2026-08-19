using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Infrastructure.PayrollEngine.Interfaces;

namespace Payroll.Infrastructure.PayrollEngine.Calculators;

/// <summary>UIF = 1% employee + 1% employer, capped at monthly earnings ceiling from TaxYear.</summary>
public class UIFCalculator : IPayrollCalculator
{
    public int Order => 110;

    public Task CalculateAsync(PayrollContext context, CancellationToken ct = default)
    {
        var ceiling = context.TaxYear.UifMonthlyEarningsCeiling;
        var rate = context.TaxYear.UifContributionRate / 100m;
        var remuneration = Math.Min(context.TaxableIncome, ceiling);
        var contribution = Math.Round(remuneration * rate, 2);

        context.Deductions.Add(new Deduction
        {
            Category = DeductionCategory.Uif,
            Description = "UIF",
            EmployeeAmount = contribution,
            EmployerAmount = contribution // employer matches employee
        });

        return Task.CompletedTask;
    }
}

/// <summary>SDL = 1% of gross remuneration — employer-only contribution.</summary>
public class SDLCalculator : IPayrollCalculator
{
    public int Order => 120;

    public Task CalculateAsync(PayrollContext context, CancellationToken ct = default)
    {
        var rate = context.TaxYear.SdlRate / 100m;
        var sdl = Math.Round(context.TaxableIncome * rate, 2);

        context.Deductions.Add(new Deduction
        {
            Category = DeductionCategory.Sdl,
            Description = "SDL",
            EmployeeAmount = 0,
            EmployerAmount = sdl
        });

        return Task.CompletedTask;
    }
}
