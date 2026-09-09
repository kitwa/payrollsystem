using Payroll.Domain.Common;

namespace Payroll.Domain.Companies;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? UifNumber { get; set; }
    public string? SdlNumber { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? PostalAddress { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public bool IsUifEnabled { get; set; } = true;
    public bool IsSdlEnabled { get; set; } = true;
    public List<Employees.Employee> Employees { get; set; } = [];
}
