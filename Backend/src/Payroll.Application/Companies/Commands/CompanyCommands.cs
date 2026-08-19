using MediatR;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Companies.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Companies.Commands;

public record UpdateCompanyCommand(Guid Id, UpdateCompanyDto Dto) : IRequest<Result>;

public class UpdateCompanyHandler(IAppDbContext db) : IRequestHandler<UpdateCompanyCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var company = await db.Companies.FindAsync([request.Id], ct);
        if (company is null) return Result.Fail("Company not found.");

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
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
