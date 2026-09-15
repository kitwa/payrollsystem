namespace Payroll.Application.Common.Interfaces;

/// <summary>Mailbox alias to send "from". SMTP authentication always uses the single configured mailbox regardless of this value.</summary>
public enum EmailSenderType
{
    Info,
    Support,
    Billing,
    System
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, EmailSenderType senderType = EmailSenderType.Info, CancellationToken ct = default);
    Task SendPayslipAsync(string to, string employeeName, byte[] pdfBytes, string filename, EmailSenderType senderType = EmailSenderType.Billing, CancellationToken ct = default);
}
