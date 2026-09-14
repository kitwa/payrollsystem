using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;

namespace Payroll.Application.Common;

/// <summary>Emails a new-account holder a link to the existing reset-password page so they can create their
/// own password before first login. Reused by employee self-login creation and admin-created user accounts.</summary>
public static class AccountInviteEmailer
{
    public static async Task SendSetPasswordEmailAsync(
        UserManager<AppUser> userManager,
        IEmailService emailService,
        ISupportNotificationSettings notificationSettings,
        ILogger logger,
        AppUser user,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return;
        try
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            var baseUrl = notificationSettings.FrontendBaseUrl.TrimEnd('/');
            var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(encodedToken)}";

            await emailService.SendAsync(user.Email, "Welcome to Payroll SA — create your password",
                $"""
                <p>Hi {user.FirstName},</p>
                <p>An account has been created for you on Payroll SA. Please create your own password before logging in.</p>
                <p><a href="{link}">Create your password</a></p>
                <p>If you weren't expecting this email, you can safely ignore it.</p>
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Account invite email failed for user {UserId}.", user.Id);
        }
    }
}
