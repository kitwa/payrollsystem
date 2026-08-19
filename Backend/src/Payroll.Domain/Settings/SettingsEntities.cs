using Payroll.Domain.Common;

namespace Payroll.Domain.Settings;

public class LeaveType : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultEntitlementDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class EarningType : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsTaxable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class DeductionType : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsEmployerContribution { get; set; }
    public bool IsActive { get; set; } = true;
}
