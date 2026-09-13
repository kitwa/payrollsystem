using System.Net;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payroll.Application.Auth.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Billing;
using Payroll.Domain.Companies;
using Payroll.Domain.Employees;
using Payroll.Domain.Identity;
using Payroll.Domain.Settings;
using Payroll.Shared;

namespace Payroll.Application.Auth.Commands;

public record RegisterCompanyCommand(RegisterCompanyDto Dto) : IRequest<Result<AuthResponseDto>>;

public class RegisterCompanyHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    IEmailService emailService,
    ILogger<RegisterCompanyHandler> logger) : IRequestHandler<RegisterCompanyCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCompanyCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var adminEmail = dto.AdminEmail.Trim().ToLowerInvariant();
        var companyName = dto.CompanyName.Trim();
        var registrationNumber = dto.RegistrationNumber.Trim();

        if (string.IsNullOrWhiteSpace(companyName))
            return Result<AuthResponseDto>.Fail("Company name is required.");

        if (string.IsNullOrWhiteSpace(registrationNumber))
            return Result<AuthResponseDto>.Fail("Registration number is required.");

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return Result<AuthResponseDto>.Fail("A user with this admin email already exists.");

        var registrationExists = await db.Companies.AnyAsync(
            c => c.RegistrationNumber.ToLower() == registrationNumber.ToLower(), ct);

        if (registrationExists)
            return Result<AuthResponseDto>.Fail("A company with this registration number already exists.");

        var company = new Company
        {
            Name = companyName,
            RegistrationNumber = registrationNumber,
            TaxNumber = string.IsNullOrWhiteSpace(dto.TaxNumber) ? null : dto.TaxNumber.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.CompanyEmail) ? null : dto.CompanyEmail.Trim().ToLowerInvariant(),
            IsActive = true
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync(ct);

        // Every company gets a protected default department so employees always have one to select.
        db.Departments.Add(new Department { CompanyId = company.Id, Name = "General", IsSystemDepartment = true });
        db.EarningTypes.AddRange(
            new EarningType { CompanyId = company.Id, Name = "Bonus", Code = "BONUS", IsTaxable = true },
            new EarningType { CompanyId = company.Id, Name = "Overtime", Code = "OVERTIME", IsTaxable = true },
            new EarningType { CompanyId = company.Id, Name = "Commission", Code = "COMMISSION", IsTaxable = true });
        db.DeductionTypes.AddRange(
            new DeductionType { CompanyId = company.Id, Name = "Staff Loan", Code = "LOAN", IsEmployerContribution = false },
            new DeductionType { CompanyId = company.Id, Name = "Medical Aid", Code = "MEDICAL_AID", IsEmployerContribution = true },
            new DeductionType { CompanyId = company.Id, Name = "Pension", Code = "PENSION", IsEmployerContribution = true },
            new DeductionType { CompanyId = company.Id, Name = "Salary Advance", Code = "ADVANCE", IsEmployerContribution = false });
        await db.SaveChangesAsync(ct);

        // First month free, all features included, capped at 5 employees until upgraded.
        var trialStart = DateTime.UtcNow;
        db.CompanySubscriptions.Add(new CompanySubscription
        {
            CompanyId = company.Id,
            PlanCode = PlanCatalog.FreeTrialCode,
            Status = SubscriptionStatus.FreeTrial,
            TrialStartDate = trialStart,
            TrialEndDate = trialStart.AddMonths(1),
            Price = 0
        });
        await db.SaveChangesAsync(ct);

        var user = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = dto.AdminFirstName.Trim(),
            LastName = dto.AdminLastName.Trim(),
            CompanyId = company.Id,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            db.Companies.Remove(company);
            await db.SaveChangesAsync(ct);
            return Result<AuthResponseDto>.Fail(createResult.Errors.Select(e => e.Description));
        }

        await userManager.AddToRoleAsync(user, Constants.Roles.Admin);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();

        user.RefreshTokens.RemoveAll(t => !t.IsActive);
        user.RefreshTokens.Add(refreshToken);
        await userManager.UpdateAsync(user);

        await SendRegistrationNotificationsAsync(company, user, ct);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto(
            accessToken,
            refreshToken.Token,
            refreshToken.Expires,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            user.Id,
            user.CompanyId,
            user.EmployeeId));
    }

    private async Task SendRegistrationNotificationsAsync(Company company, AppUser admin, CancellationToken ct)
    {
        try
        {
            await emailService.SendAsync(admin.Email!, "Welcome to Payroll SA",
                $"""
                <p>Hi {WebUtility.HtmlEncode(admin.FirstName)},</p>
                <p>Welcome to Payroll SA! Your company <strong>{WebUtility.HtmlEncode(company.Name)}</strong> has been registered successfully and your free trial has started.</p>
                <p>You can now log in to add employees, run payroll, and manage leave.</p>
                <p>Regards,<br/>The Payroll SA Team</p>
                """, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Welcome email failed for company {CompanyId}.", company.Id);
        }

        try
        {
            var superAdmins = await userManager.GetUsersInRoleAsync(Constants.Roles.SuperAdmin);
            var body = $"""
                <p>A new company has registered on Payroll SA.</p>
                <p><strong>Company:</strong> {WebUtility.HtmlEncode(company.Name)}<br/>
                <strong>Registration Number:</strong> {WebUtility.HtmlEncode(company.RegistrationNumber)}<br/>
                <strong>Admin:</strong> {WebUtility.HtmlEncode(admin.FirstName)} {WebUtility.HtmlEncode(admin.LastName)} ({WebUtility.HtmlEncode(admin.Email!)})<br/>
                <strong>Phone:</strong> {WebUtility.HtmlEncode(company.Phone ?? "\u2014")}<br/>
                <strong>Registered:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
                """;

            foreach (var superAdmin in superAdmins.Where(a => a.IsActive && !string.IsNullOrWhiteSpace(a.Email)))
                await emailService.SendAsync(superAdmin.Email!, $"New Company Registered: {company.Name}", body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SuperAdmin registration notification failed for company {CompanyId}.", company.Id);
        }
    }
}
