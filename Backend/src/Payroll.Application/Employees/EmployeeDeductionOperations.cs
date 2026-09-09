using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Shared;

namespace Payroll.Application.Employees;

public record EmployeeDeductionDto(
    Guid Id, Guid EmployeeId, string Description, DeductionCategory Category,
    decimal EmployeeAmount, decimal EmployerAmount, bool IsActive);

public record GetEmployeeDeductionsQuery(Guid CompanyId, Guid EmployeeId)
    : IRequest<Result<List<EmployeeDeductionDto>>>;

public class GetEmployeeDeductionsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeeDeductionsQuery, Result<List<EmployeeDeductionDto>>>
{
    public async Task<Result<List<EmployeeDeductionDto>>> Handle(GetEmployeeDeductionsQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanManageCompany(currentUser, request.CompanyId))
            return Result<List<EmployeeDeductionDto>>.Fail("You are not authorized to view employee deductions.");

        var result = await db.EmployeeDeductions
            .Where(d => d.CompanyId == request.CompanyId && d.EmployeeId == request.EmployeeId && !d.IsDeleted)
            .OrderBy(d => d.Description)
            .Select(d => new EmployeeDeductionDto(d.Id, d.EmployeeId, d.Description, d.Category,
                d.EmployeeAmount, d.EmployerAmount, d.IsActive))
            .ToListAsync(ct);

        return Result<List<EmployeeDeductionDto>>.Ok(result);
    }
}

public record CreateEmployeeDeductionDto(
    Guid CompanyId, Guid EmployeeId, string Description, DeductionCategory Category,
    decimal EmployeeAmount, decimal EmployerAmount);

public record CreateEmployeeDeductionCommand(CreateEmployeeDeductionDto Dto) : IRequest<Result<Guid>>;

public class CreateEmployeeDeductionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateEmployeeDeductionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeDeductionCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to create employee deductions.");
        if (string.IsNullOrWhiteSpace(d.Description))
            return Result<Guid>.Fail("Deduction description is required.");
        if (d.EmployeeAmount < 0 || d.EmployerAmount < 0)
            return Result<Guid>.Fail("Deduction amounts cannot be negative.");

        var employeeExists = await db.Employees.AnyAsync(
            e => e.Id == d.EmployeeId && e.CompanyId == d.CompanyId && !e.IsDeleted, ct);
        if (!employeeExists) return Result<Guid>.Fail("Employee not found in this company.");

        var deduction = new EmployeeDeduction
        {
            CompanyId = d.CompanyId,
            EmployeeId = d.EmployeeId,
            Description = d.Description.Trim(),
            Category = d.Category,
            EmployeeAmount = d.EmployeeAmount,
            EmployerAmount = d.EmployerAmount,
            IsActive = true
        };
        db.EmployeeDeductions.Add(deduction);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(deduction.Id);
    }
}

public record UpdateEmployeeDeductionDto(
    string Description, DeductionCategory Category, decimal EmployeeAmount,
    decimal EmployerAmount, bool IsActive);

public record UpdateEmployeeDeductionCommand(Guid Id, UpdateEmployeeDeductionDto Dto) : IRequest<Result>;

public class UpdateEmployeeDeductionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateEmployeeDeductionCommand, Result>
{
    public async Task<Result> Handle(UpdateEmployeeDeductionCommand request, CancellationToken ct)
    {
        var deduction = await db.EmployeeDeductions.FirstOrDefaultAsync(
            d => d.Id == request.Id && !d.IsDeleted, ct);
        if (deduction is null) return Result.Fail("Employee deduction not found.");
        if (!TenantAccess.CanManageCompany(currentUser, deduction.CompanyId))
            return Result.Fail("You are not authorized to update this deduction.");
        if (string.IsNullOrWhiteSpace(request.Dto.Description))
            return Result.Fail("Deduction description is required.");
        if (request.Dto.EmployeeAmount < 0 || request.Dto.EmployerAmount < 0)
            return Result.Fail("Deduction amounts cannot be negative.");

        deduction.Description = request.Dto.Description.Trim();
        deduction.Category = request.Dto.Category;
        deduction.EmployeeAmount = request.Dto.EmployeeAmount;
        deduction.EmployerAmount = request.Dto.EmployerAmount;
        deduction.IsActive = request.Dto.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
