using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

/// <summary>Value object — owned by Employee, no separate DbSet needed.</summary>
public class BankDetails : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // Cheque, Savings, Transmission
}
