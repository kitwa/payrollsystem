using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Payroll.Application.Common.Interfaces;

namespace Payroll.Infrastructure.Services;

/// <summary>SMTP email delivery. When EmailSettings:Enabled is false the message is logged instead of sent.</summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    private bool Enabled => config.GetValue("EmailSettings:Enabled", false);
    private string Host => config["EmailSettings:Host"] ?? string.Empty;
    private int Port => config.GetValue("EmailSettings:Port", 587);
    private string User => config["EmailSettings:User"] ?? string.Empty;
    private string Password => config["EmailSettings:Password"] ?? string.Empty;
    private string FromAddress => config["EmailSettings:From"] ?? User;
    private string FromName => config["EmailSettings:FromName"] ?? "Payroll SA";

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default) =>
        SendMessageAsync(to, subject, htmlBody, null, null, ct);

    public Task SendPayslipAsync(string to, string employeeName, byte[] pdfBytes, string filename, CancellationToken ct = default)
    {
        var subject = "Your payslip is available";
        var body = $"""
            <p>Hi {employeeName},</p>
            <p>Your latest payslip is attached to this email.</p>
            <p>Please keep it safe — it contains confidential payroll information.</p>
            <p>Regards,<br/>{FromName}</p>
            """;

        return SendMessageAsync(to, subject, body, pdfBytes, filename, ct);
    }

    private async Task SendMessageAsync(
        string to, string subject, string htmlBody, byte[]? attachment, string? attachmentName, CancellationToken ct)
    {
        if (!Enabled)
        {
            logger.LogWarning("Email delivery disabled. '{Subject}' was not sent to {Recipient}.", subject, to);
            throw new InvalidOperationException("Email delivery is disabled. Configure EmailSettings and set Enabled to true.");
        }

        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("EmailSettings:Host is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(FromName, FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        if (attachment is not null && attachmentName is not null)
            builder.Attachments.Add(attachmentName, attachment, new ContentType("application", "pdf"));

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(Host, Port, SecureSocketOptions.StartTlsWhenAvailable, ct);

        if (!string.IsNullOrWhiteSpace(User))
            await client.AuthenticateAsync(User, Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
