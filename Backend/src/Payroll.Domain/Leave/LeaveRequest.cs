using Payroll.Domain.Common;
using Payroll.Domain.Leave.Enums;

namespace Payroll.Domain.Leave;

public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employees.Employee Employee { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public Settings.LeaveType LeaveType { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public class LeaveBalance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employees.Employee Employee { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public Settings.LeaveType LeaveType { get; set; } = null!;
    public decimal EntitlementDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal BalanceDays => EntitlementDays - UsedDays;
    public int Year { get; set; }
}
