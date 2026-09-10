using FluentAssertions;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Domain.Tax;
using Payroll.Infrastructure.PayrollEngine;
using Payroll.Infrastructure.PayrollEngine.Calculators;

namespace Payroll.Application.Tests;

public class PAYECalculatorTests
{
    private static readonly DateTime TaxYearEnd = new(2027, 2, 28);

    [Theory]
    [InlineData(89000, 0)]
    [InlineData(99000, 0)]
    [InlineData(245100, 2191.50)]
    [InlineData(245101, 2191.52)]
    [InlineData(383100, 5181.50)]
    [InlineData(383101, 5181.53)]
    [InlineData(530200, 8981.58)]
    [InlineData(530201, 8981.61)]
    [InlineData(695800, 13949.58)]
    [InlineData(695801, 13949.62)]
    [InlineData(887000, 20163.58)]
    [InlineData(887001, 20163.62)]
    [InlineData(1878600, 54043.25)]
    [InlineData(1878601, 54043.29)]
    public async Task CalculatesProgressiveMonthlyPaye(decimal annualTaxableIncome, decimal expectedMonthlyPaye)
    {
        var context = CreateContext(annualTaxableIncome / 12m);

        await new PAYECalculator().CalculateAsync(context);

        var paye = context.Deductions.Single(d => d.Category == DeductionCategory.Paye);
        paye.EmployeeAmount.Should().Be(expectedMonthlyPaye);
    }

    [Fact]
    public async Task UsesTaxableIncomeAndExcludesNonTaxableAmountsFromPayeBase()
    {
        var context = CreateContext(300000m / 12m);
        context.Earnings.Add(new global::Payroll.Domain.PayrollRuns.Earning
        {
            Category = EarningCategory.Bonus,
            Description = "Non-taxable benefit",
            Amount = 100000,
            IsTaxable = false
        });

        await new PAYECalculator().CalculateAsync(context);

        var paye = context.Deductions.Single(d => d.Category == DeductionCategory.Paye);
        paye.EmployeeAmount.Should().Be(3381.00m);
    }

    private static PayrollContext CreateContext(decimal monthlyTaxableIncome)
    {
        var taxYear = new TaxYear
        {
            Year = 2026,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = TaxYearEnd,
            TaxTables =
            [
                new TaxTable { IncomeFrom = 0, IncomeTo = 245100, BaseTax = 0, MarginalRate = 18 },
                new TaxTable { IncomeFrom = 245100, IncomeTo = 383100, BaseTax = 44118, MarginalRate = 26 },
                new TaxTable { IncomeFrom = 383100, IncomeTo = 530200, BaseTax = 79998, MarginalRate = 31 },
                new TaxTable { IncomeFrom = 530200, IncomeTo = 695800, BaseTax = 125599, MarginalRate = 36 },
                new TaxTable { IncomeFrom = 695800, IncomeTo = 887000, BaseTax = 185215, MarginalRate = 39 },
                new TaxTable { IncomeFrom = 887000, IncomeTo = 1878600, BaseTax = 259783, MarginalRate = 41 },
                new TaxTable { IncomeFrom = 1878600, IncomeTo = decimal.MaxValue, BaseTax = 666339, MarginalRate = 45 }
            ],
            TaxThresholds = [new TaxThreshold { AgeGroup = "Under65", ThresholdAmount = 99000 }],
            TaxRebates = [new TaxRebate { RebateType = "Primary", Amount = 17820 }]
        };

        return new PayrollContext
        {
            DateOfBirth = new DateTime(1990, 1, 1),
            TaxableIncome = monthlyTaxableIncome,
            TaxYear = taxYear
        };
    }
}