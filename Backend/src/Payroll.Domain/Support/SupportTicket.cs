using Payroll.Domain.Common;

namespace Payroll.Domain.Support;

public enum SupportTicketType
{
    TechnicalIssue,
    BugReport,
    FeatureRequest,
    PayrollQuestion,
    Other
}

public enum SupportTicketStatus
{
    Open,
    InReview,
    InProgress,
    Resolved,
    Closed
}

public class SupportTicket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportTicketType Type { get; set; }
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    /// <summary>False when a Super Admin status change hasn't yet been seen by the company that raised the ticket.</summary>
    public bool IsReadByCompany { get; set; } = true;
}