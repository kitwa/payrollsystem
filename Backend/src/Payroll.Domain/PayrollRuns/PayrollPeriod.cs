using Payroll.Domain.Common;
using Payroll.Domain.PayrollRuns.Enums;

namespace Payroll.Domain.PayrollRuns;

public class PayrollPeriod : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Companies.Company Company { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<PayrollLine> Lines { get; set; } = [];

    /// <summary>Validates state machine — only forward transitions allowed.</summary>
    public bool CanTransitionTo(PayrollStatus next) => (Status, next) switch
    {
        (PayrollStatus.Draft, PayrollStatus.Approved) => true,
        (PayrollStatus.Approved, PayrollStatus.Locked) => true,
        (PayrollStatus.Locked, PayrollStatus.Paid) => true,
        _ => false
    };
}
