namespace Payroll.Infrastructure.PayrollEngine;

/// <summary>Immutable context passed to every calculator — calculators must not call DbContext directly.</summary>
public class PayrollContext
{
    public Guid EmployeeId { get; init; }
    public Guid PeriodId { get; init; }
    public decimal BasicSalary { get; init; }
    public DateTime DateOfBirth { get; init; }
    public decimal TaxableIncome { get; set; }
    public Domain.Tax.TaxYear TaxYear { get; init; } = null!;
    public List<Domain.PayrollRuns.Earning> Earnings { get; } = [];
    public List<Domain.PayrollRuns.Deduction> Deductions { get; } = [];
}
