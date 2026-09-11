using Payroll.Domain.Common;
using Payroll.Domain.Companies;
using Payroll.Domain.Employees;

namespace Payroll.Domain.Tax;

public enum TaxCertificateType { IRP5, IT3A }
public enum TaxCertificateStatus { Draft, Generated, Finalized }

public class EmployeeTaxCertificate : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid TaxYearId { get; set; }
    public TaxYear TaxYear { get; set; } = null!;
    public TaxCertificateType CertificateType { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public TaxCertificateStatus Status { get; set; } = TaxCertificateStatus.Generated;
    public decimal GrossRemuneration { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal PAYE { get; set; }
    public decimal UIF { get; set; }
    public decimal SDL { get; set; }
    public decimal Allowances { get; set; }
    public decimal Bonuses { get; set; }
    public decimal Benefits { get; set; }
    public decimal RetirementContributions { get; set; }
    public decimal ProvidentContributions { get; set; }
    public decimal PensionContributions { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public Guid? GeneratedBy { get; set; }
    public bool IsFinal { get; set; }
    public List<TaxCertificateLine> Lines { get; set; } = [];
}

public class TaxCertificateLine : BaseEntity
{
    public Guid CertificateId { get; set; }
    public EmployeeTaxCertificate Certificate { get; set; } = null!;
    public string SARSCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
}