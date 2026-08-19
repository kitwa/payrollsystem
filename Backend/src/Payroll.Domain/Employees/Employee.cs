using Payroll.Domain.Common;
using Payroll.Domain.Employees.Enums;

namespace Payroll.Domain.Employees;

public class Employee : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Companies.Company Company { get; set; } = null!;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string? PassportNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? UifNumber { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;
    public EmploymentType EmploymentType { get; set; }
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Monthly;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public decimal BasicSalary { get; set; }
    public BankDetails? BankDetails { get; set; }
    public List<EmployeeDocument> Documents { get; set; } = [];
    public List<Leave.LeaveBalance> LeaveBalances { get; set; } = [];
    public List<Leave.LeaveRequest> LeaveRequests { get; set; } = [];
}
