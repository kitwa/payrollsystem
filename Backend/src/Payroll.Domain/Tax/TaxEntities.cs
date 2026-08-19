using Payroll.Domain.Common;

namespace Payroll.Domain.Tax;

/// <summary>Progressive tax bracket — loaded from DB, never hardcoded.</summary>
public class TaxTable : BaseEntity
{
    public Guid TaxYearId { get; set; }
    public TaxYear TaxYear { get; set; } = null!;
    public decimal IncomeFrom { get; set; }
    public decimal IncomeTo { get; set; }
    public decimal BaseTax { get; set; }
    public decimal MarginalRate { get; set; } // percentage e.g. 18 = 18%
}

public class TaxThreshold : BaseEntity
{
    public Guid TaxYearId { get; set; }
    public TaxYear TaxYear { get; set; } = null!;
    public string AgeGroup { get; set; } = string.Empty; // Under65, 65to74, 75AndOver
    public decimal ThresholdAmount { get; set; }
}

public class TaxRebate : BaseEntity
{
    public Guid TaxYearId { get; set; }
    public TaxYear TaxYear { get; set; } = null!;
    public string RebateType { get; set; } = string.Empty; // Primary, Secondary, Tertiary
    public decimal Amount { get; set; }
}

public class TaxYear : BaseEntity
{
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal UifMonthlyEarningsCeiling { get; set; }
    public decimal UifContributionRate { get; set; } // % e.g. 1 = 1%
    public decimal SdlRate { get; set; } // % e.g. 1 = 1%
    public bool IsActive { get; set; }
    public List<TaxTable> TaxTables { get; set; } = [];
    public List<TaxThreshold> TaxThresholds { get; set; } = [];
    public List<TaxRebate> TaxRebates { get; set; } = [];
}
