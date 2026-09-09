using Payroll.Domain.Common;

namespace Payroll.Domain.Audit;

public class AuditLog : BaseEntity
{
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Action { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
