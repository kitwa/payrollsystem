using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Infrastructure.PayrollEngine.Interfaces;

namespace Payroll.Infrastructure.PayrollEngine.Calculators;

/// <summary>Calculates PAYE using progressive tax tables loaded from TaxYear — never hardcoded.</summary>
public class PAYECalculator : IPayrollCalculator
{
    public int Order => 100;

    public Task CalculateAsync(PayrollContext context, CancellationToken ct = default)
    {
        var annualIncome = context.TaxableIncome * 12;
        var taxYear = context.TaxYear;

        var bracket = taxYear.TaxTables
            .OrderBy(t => t.IncomeFrom)
            .LastOrDefault(t => annualIncome >= t.IncomeFrom);

        if (bracket is null) return Task.CompletedTask;

        var annualTax = bracket.BaseTax + (annualIncome - bracket.IncomeFrom) * (bracket.MarginalRate / 100m);

        // Apply primary rebate (age determines which rebates apply)
        var age = CalculateAge(context.DateOfBirth);
        var primaryRebate = taxYear.TaxRebates.FirstOrDefault(r => r.RebateType == "Primary")?.Amount ?? 0;
        var secondaryRebate = age >= 65 ? (taxYear.TaxRebates.FirstOrDefault(r => r.RebateType == "Secondary")?.Amount ?? 0) : 0;
        var tertiaryRebate = age >= 75 ? (taxYear.TaxRebates.FirstOrDefault(r => r.RebateType == "Tertiary")?.Amount ?? 0) : 0;

        annualTax -= primaryRebate + secondaryRebate + tertiaryRebate;
        if (annualTax < 0) annualTax = 0;

        var monthlyPaye = Math.Round(annualTax / 12, 2);

        context.Deductions.Add(new Deduction
        {
            Category = DeductionCategory.Paye,
            Description = "PAYE",
            EmployeeAmount = monthlyPaye,
            EmployerAmount = 0
        });

        return Task.CompletedTask;
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}
