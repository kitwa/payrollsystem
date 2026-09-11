using FluentAssertions;
using Payroll.Domain.Tax;

namespace Payroll.Domain.Tests;

public class TaxCertificateRulesTests
{
    [Fact]
    public void South_African_tax_year_includes_first_march_and_last_february_day()
    {
        var year = new TaxYear
        {
            StartDate = new DateTime(2025, 3, 1),
            EndDate = new DateTime(2026, 2, 28)
        };

        year.ContainsPayrollPeriod(new DateTime(2025, 3, 1), new DateTime(2025, 3, 31)).Should().BeTrue();
        year.ContainsPayrollPeriod(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)).Should().BeTrue();
    }

    [Fact]
    public void South_African_tax_year_excludes_periods_outside_the_boundary()
    {
        var year = new TaxYear
        {
            StartDate = new DateTime(2025, 3, 1),
            EndDate = new DateTime(2026, 2, 28)
        };

        year.ContainsPayrollPeriod(new DateTime(2025, 2, 1), new DateTime(2025, 2, 28)).Should().BeFalse();
        year.ContainsPayrollPeriod(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)).Should().BeFalse();
    }
}