using Payroll.Domain.Common;

namespace Payroll.Domain.PayrollRuns;

/// <summary>An admin-added, period-specific bonus earning for one employee. Applied as an Earning by the payroll engine.</summary>
public class EmployeeBonus : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employees.Employee Employee { get; set; } = null!;
    public Guid? EarningTypeId { get; set; }
    public Settings.EarningType? EarningType { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
