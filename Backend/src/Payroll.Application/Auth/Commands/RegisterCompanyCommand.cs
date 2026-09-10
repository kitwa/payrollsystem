using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Auth.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Billing;
using Payroll.Domain.Companies;
using Payroll.Domain.Employees;
using Payroll.Domain.Identity;
using Payroll.Shared;

namespace Payroll.Application.Auth.Commands;

public record RegisterCompanyCommand(RegisterCompanyDto Dto) : IRequest<Result<AuthResponseDto>>;

public class RegisterCompanyHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ITokenService tokenService) : IRequestHandler<RegisterCompanyCommand, Result<AuthResponseDto>>
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
}
