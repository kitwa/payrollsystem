using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Employees.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Employees.Queries;

public record GetEmployeesQuery(Guid CompanyId, PaginationParams Params) : IRequest<Result<PagedList<EmployeeListDto>>>;

public class GetEmployeesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetEmployeesQuery, Result<PagedList<EmployeeListDto>>>
{
    public async Task<Result<PagedList<EmployeeListDto>>> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<PagedList<EmployeeListDto>>.Fail("You are not authorized to view this company's employees.");

        var employees = await db.Employees
            .Where(e => e.CompanyId == request.CompanyId && !e.IsDeleted)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListDto(e.Id, e.EmployeeNumber, e.FirstName, e.LastName,
                e.Email, e.JobTitle, e.Department, e.Status, e.StartDate))
            .ToListAsync(ct);

        return Result<PagedList<EmployeeListDto>>.Ok(
            PagedList<EmployeeListDto>.Create(employees, request.Params.PageNumber, request.Params.PageSize));
    }
}

public record GetEmployeeByIdQuery(Guid Id) : IRequest<Result<EmployeeDto>>;

public class GetEmployeeByIdHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery request, CancellationToken ct)
    {
        var e = await db.Employees
            .Include(e => e.BankDetails)
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, ct);

        if (e is null) return Result<EmployeeDto>.Fail("Employee not found.");
        if (!TenantAccess.CanAccessEmployee(currentUser, e.CompanyId, e.Id))
            return Result<EmployeeDto>.Fail("You are not authorized to view this employee.");

        var bd = e.BankDetails is null ? null
            : new BankDetailsDto(e.BankDetails.BankName, e.BankDetails.AccountNumber,
                e.BankDetails.BranchCode, e.BankDetails.AccountType);

        return Result<EmployeeDto>.Ok(new EmployeeDto(e.Id, e.CompanyId, e.EmployeeNumber,
            e.FirstName, e.LastName, e.IdNumber, e.DateOfBirth, e.Gender,
            e.Email, e.Phone, e.Address, e.TaxNumber, e.UifNumber,
            e.Status, e.EmploymentType, e.PayFrequency,
            e.JobTitle, e.Department, e.StartDate, e.TerminationDate, e.BasicSalary, bd));
    }
}
