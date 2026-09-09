using Payroll.Domain.Common;

namespace Payroll.Domain.PayrollRuns;

public class PayrollLine : BaseEntity
{
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employees.Employee Employee { get; set; } = null!;
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public DateTime? PayslipEmailedAt { get; set; }
    public string? PayslipEmailedTo { get; set; }
    public List<Earning> Earnings { get; set; } = [];
    public List<Deduction> Deductions { get; set; } = [];
}
