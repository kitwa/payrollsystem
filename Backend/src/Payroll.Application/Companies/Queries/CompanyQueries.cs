using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Companies.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Companies.Queries;

public record GetCompanyByIdQuery(Guid Id) : IRequest<Result<CompanyDto>>;

public class GetCompanyByIdHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetCompanyByIdQuery, Result<CompanyDto>>
{
    public async Task<Result<CompanyDto>> Handle(GetCompanyByIdQuery request, CancellationToken ct)
    {
        var c = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);
        if (c is null) return Result<CompanyDto>.Fail("Company not found.");
        if (!TenantAccess.CanAccessCompany(currentUser, c.Id))
            return Result<CompanyDto>.Fail("You are not authorized to view this company.");

        return Result<CompanyDto>.Ok(new CompanyDto(c.Id, c.Name, c.RegistrationNumber, c.TaxNumber,
            c.UifNumber, c.SdlNumber, c.PhysicalAddress, c.PostalAddress, c.Phone, c.Email, c.IsActive,
            c.LogoData != null, c.IsUifEnabled, c.IsSdlEnabled));
    }
}

public record GetCompaniesQuery : IRequest<Result<List<CompanyDto>>>;

public class GetCompaniesHandler(IAppDbContext db) : IRequestHandler<GetCompaniesQuery, Result<List<CompanyDto>>>
{
    public async Task<Result<List<CompanyDto>>> Handle(GetCompaniesQuery request, CancellationToken ct)
    {
        var companies = await db.Companies
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Name, c.RegistrationNumber, c.TaxNumber,
                c.UifNumber, c.SdlNumber, c.PhysicalAddress, c.PostalAddress, c.Phone, c.Email, c.IsActive,
                c.LogoData != null, c.IsUifEnabled, c.IsSdlEnabled))
            .ToListAsync(ct);

        return Result<List<CompanyDto>>.Ok(companies);
    }
}

public record CompanyLogoDto(byte[] Data, string ContentType);

public record GetCompanyLogoQuery(Guid Id) : IRequest<Result<CompanyLogoDto>>;

public class GetCompanyLogoHandler(IAppDbContext db) : IRequestHandler<GetCompanyLogoQuery, Result<CompanyLogoDto>>
{
    public async Task<Result<CompanyLogoDto>> Handle(GetCompanyLogoQuery request, CancellationToken ct)
    {
        var c = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);
        if (c?.LogoData is null || c.LogoContentType is null)
            return Result<CompanyLogoDto>.Fail("This company has no logo.");

        return Result<CompanyLogoDto>.Ok(new CompanyLogoDto(c.LogoData, c.LogoContentType));
    }
}
