using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Infrastructure.PayrollEngine.Interfaces;

namespace Payroll.Infrastructure.PayrollEngine.Calculators;

/// <summary>Calculates monthly PAYE from annual progressive tax tables loaded from TaxYear.</summary>
public class PAYECalculator : IPayrollCalculator
{
    public int Order => 100;

    public Task CalculateAsync(PayrollContext context, CancellationToken ct = default)
    {
        var taxYear = context.TaxYear;
        var annualIncome = Math.Max(0, context.TaxableIncome * 12);
        var age = CalculateAge(context.DateOfBirth, taxYear.EndDate);
        var ageGroup = age >= 75 ? "75AndOver" : age >= 65 ? "65to74" : "Under65";
        var threshold = taxYear.TaxThresholds
            .FirstOrDefault(t => t.AgeGroup == ageGroup)?.ThresholdAmount ?? 0;

        var annualTax = annualIncome <= threshold
            ? 0
            : CalculateTaxFromBrackets(annualIncome, taxYear);

        // Apply primary rebate (age determines which rebates apply)
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

    internal static decimal CalculateTaxFromBrackets(decimal annualIncome, Domain.Tax.TaxYear taxYear)
    {
        var bracket = taxYear.TaxTables
            .OrderBy(t => t.IncomeFrom)
            .LastOrDefault(t => annualIncome >= t.IncomeFrom);

        if (bracket is null) return 0;

        return bracket.BaseTax + (annualIncome - bracket.IncomeFrom) * (bracket.MarginalRate / 100m);
    }

    private static int CalculateAge(DateTime dateOfBirth, DateTime asOf)
    {
        var age = asOf.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > asOf.AddYears(-age)) age--;
        return age;
    }
}
