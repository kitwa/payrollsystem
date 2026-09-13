using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payroll.Application.Auth.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;
using Payroll.Shared;

namespace Payroll.Application.Auth.Commands;

public record ChangePasswordCommand(ChangePasswordDto Dto) : IRequest<Result>;

public class ChangePasswordHandler(
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IEmailService emailService,
    ILogger<ChangePasswordHandler> logger) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null || !user.IsActive) return Result.Fail("Unable to change password.");
        var result = await userManager.ChangePasswordAsync(user, request.Dto.CurrentPassword, request.Dto.NewPassword);
        if (!result.Succeeded) return Result.Fail(result.Errors.Select(e => e.Description));

        await PasswordChangeNotifier.SendConfirmationAsync(emailService, logger, user);
        return Result.Ok();
    }
}

public record ForgotPasswordCommand(ForgotPasswordDto Dto) : IRequest<Result>;

public class ForgotPasswordHandler(
    UserManager<AppUser> userManager,
    IEmailService emailService,
    IConfiguration configuration) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var generic = Result.Ok();
        var email = request.Dto.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive) return generic;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        var baseUrl = configuration["SupportSettings:FrontendBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";
        await emailService.SendAsync(email, "Reset your Payroll SA password",
            $"<p>We received a request to reset your Payroll SA password.</p><p><a href=\"{link}\">Reset your password</a></p><p>This link expires according to your Identity token settings. If you did not request this, you can ignore this email.</p>", ct);
        return generic;
    }
}

public record ResetPasswordCommand(ResetPasswordDto Dto) : IRequest<Result>;

public class ResetPasswordHandler(
    UserManager<AppUser> userManager,
    IEmailService emailService,
    ILogger<ResetPasswordHandler> logger) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var email = request.Dto.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive) return Result.Fail("The reset link is invalid or has expired.");
        string token;
        try { token = Encoding.UTF8.GetString(Convert.FromBase64String(request.Dto.Token)); }
        catch (FormatException) { return Result.Fail("The reset link is invalid or has expired."); }
        var result = await userManager.ResetPasswordAsync(user, token, request.Dto.NewPassword);
        if (!result.Succeeded) return Result.Fail(result.Errors.Select(e => e.Description));

        await PasswordChangeNotifier.SendConfirmationAsync(emailService, logger, user);
        return Result.Ok();
    }
}

/// <summary>Shared "your password has changed" confirmation email, used by both the change and reset flows.</summary>
file static class PasswordChangeNotifier
{
    public static async Task SendConfirmationAsync(IEmailService emailService, ILogger logger, AppUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return;
        try
        {
            await emailService.SendAsync(user.Email, "Your Payroll SA password has changed",
                $"""
                <p>Hi {user.FirstName},</p>
                <p>This is a confirmation that the password for your Payroll SA account was just changed.</p>
                <p>If you did not make this change, please contact your administrator immediately.</p>
                """);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Password-changed confirmation email failed for user {UserId}.", user.Id);
        }
    }
}