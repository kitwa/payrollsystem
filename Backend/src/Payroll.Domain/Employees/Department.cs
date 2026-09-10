using Payroll.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payroll.Domain.Employees;

public class Department : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Companies.Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;

    /// <summary>Marks the company's protected default department — cannot be renamed or deleted.</summary>
    public bool IsSystemDepartment { get; set; }

    /// <summary>Alias used by the business rules and UI to identify the protected company default.</summary>
    [NotMapped]
    public bool IsDefault
    {
        get => IsSystemDepartment;
        set => IsSystemDepartment = value;
    }
}
