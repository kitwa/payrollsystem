namespace Payroll.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendPayslipAsync(string to, string employeeName, byte[] pdfBytes, string filename, CancellationToken ct = default);
}
