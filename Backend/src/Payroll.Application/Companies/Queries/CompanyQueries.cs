using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Companies.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Companies.Queries;

public record GetCompanyByIdQuery(Guid Id) : IRequest<Result<CompanyDto>>;

public class GetCompanyByIdHandler(IAppDbContext db) : IRequestHandler<GetCompanyByIdQuery, Result<CompanyDto>>
{
    public async Task<Result<CompanyDto>> Handle(GetCompanyByIdQuery request, CancellationToken ct)
    {
        var c = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);
        if (c is null) return Result<CompanyDto>.Fail("Company not found.");

        return Result<CompanyDto>.Ok(new CompanyDto(c.Id, c.Name, c.RegistrationNumber, c.TaxNumber,
            c.UifNumber, c.SdlNumber, c.PhysicalAddress, c.PostalAddress, c.Phone, c.Email, c.IsActive));
    }
}
