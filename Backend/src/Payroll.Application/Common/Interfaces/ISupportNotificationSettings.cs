namespace Payroll.Application.Common.Interfaces;

public interface ISupportNotificationSettings
{
    string SupportEmail { get; }
    string FrontendBaseUrl { get; }
}
