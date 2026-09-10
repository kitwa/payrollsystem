using Microsoft.Extensions.Configuration;
using Payroll.Application.Common.Interfaces;

namespace Payroll.Infrastructure.Services;

public class SupportNotificationSettings(IConfiguration config) : ISupportNotificationSettings
{
    public string SupportEmail => config["SupportSettings:Email"] ?? config["EmailSettings:From"] ?? string.Empty;
    public string FrontendBaseUrl => config["SupportSettings:FrontendBaseUrl"] ?? "http://localhost:4200";
}
