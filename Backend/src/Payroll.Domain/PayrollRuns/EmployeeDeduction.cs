using Payroll.Domain.Common;
using Payroll.Domain.PayrollRuns.Enums;

namespace Payroll.Domain.PayrollRuns;

public class EmployeeDeduction : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employees.Employee Employee { get; set; } = null!;
    public DeductionCategory Category { get; set; } = DeductionCategory.Other;
    public string Description { get; set; } = string.Empty;
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerAmount { get; set; }
    public bool IsActive { get; set; } = true;
}
