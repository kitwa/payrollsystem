using Payroll.Domain.Common;
using Payroll.Domain.PayrollRuns.Enums;

namespace Payroll.Domain.PayrollRuns;

public class Earning : BaseEntity
{
    public Guid PayrollLineId { get; set; }
    public PayrollLine PayrollLine { get; set; } = null!;
    public EarningCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; } = true;
}

public class Deduction : BaseEntity
{
    public Guid PayrollLineId { get; set; }
    public PayrollLine PayrollLine { get; set; } = null!;
    public DeductionCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerAmount { get; set; }
}
