using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public class EmployeeDocument : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string DocumentType { get; set; } = string.Empty; // ID, Contract, Certificate, etc.
}
