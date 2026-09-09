using MediatR;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Companies.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Companies.Commands;

public record UpdateCompanyCommand(Guid Id, UpdateCompanyDto Dto) : IRequest<Result>;

public class UpdateCompanyHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateCompanyCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var company = await db.Companies.FindAsync([request.Id], ct);
        if (company is null) return Result.Fail("Company not found.");
        if (!TenantAccess.CanManageCompany(currentUser, company.Id))
            return Result.Fail("You are not authorized to update this company.");

        var d = request.Dto;
        company.Name = d.Name;
        company.RegistrationNumber = d.RegistrationNumber;
        company.TaxNumber = d.TaxNumber;
        company.UifNumber = d.UifNumber;
        company.SdlNumber = d.SdlNumber;
        company.PhysicalAddress = d.PhysicalAddress;
        company.PostalAddress = d.PostalAddress;
        company.Phone = d.Phone;
        company.Email = d.Email;
        company.IsUifEnabled = d.IsUifEnabled;
        company.IsSdlEnabled = d.IsSdlEnabled;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record UploadCompanyLogoCommand(Guid CompanyId, byte[] Data, string ContentType) : IRequest<Result>;

public class UploadCompanyLogoHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UploadCompanyLogoCommand, Result>
{
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/webp", "image/svg+xml"];

    public async Task<Result> Handle(UploadCompanyLogoCommand request, CancellationToken ct)
    {
        var company = await db.Companies.FindAsync([request.CompanyId], ct);
        if (company is null) return Result.Fail("Company not found.");
        if (!TenantAccess.CanManageCompany(currentUser, company.Id))
            return Result.Fail("You are not authorized to update this company's logo.");
        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result.Fail("Logo must be a PNG, JPEG, WEBP, or SVG image.");
        if (request.Data.Length == 0)
            return Result.Fail("Logo file is empty.");
        if (request.Data.Length > 2 * 1024 * 1024)
            return Result.Fail("Logo must be smaller than 2MB.");

        company.LogoData = request.Data;
        company.LogoContentType = request.ContentType;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record RemoveCompanyLogoCommand(Guid CompanyId) : IRequest<Result>;

public class RemoveCompanyLogoHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<RemoveCompanyLogoCommand, Result>
{
    public async Task<Result> Handle(RemoveCompanyLogoCommand request, CancellationToken ct)
    {
        var company = await db.Companies.FindAsync([request.CompanyId], ct);
        if (company is null) return Result.Fail("Company not found.");
        if (!TenantAccess.CanManageCompany(currentUser, company.Id))
            return Result.Fail("You are not authorized to update this company's logo.");

        company.LogoData = null;
        company.LogoContentType = null;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
