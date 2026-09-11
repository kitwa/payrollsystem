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
    decimal EmployeeAmount, decimal EmployerAmount, bool IsActive, Guid? DeductionTypeId, Guid? PayrollPeriodId);

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
                d.EmployeeAmount, d.EmployerAmount, d.IsActive, d.DeductionTypeId, d.PayrollPeriodId))
            .ToListAsync(ct);

        return Result<List<EmployeeDeductionDto>>.Ok(result);
    }
}

public record CreateEmployeeDeductionDto(
    Guid CompanyId, Guid EmployeeId, string Description, DeductionCategory Category,
    decimal EmployeeAmount, decimal EmployerAmount, Guid? DeductionTypeId = null, Guid? PayrollPeriodId = null);

public record CreateEmployeeDeductionCommand(CreateEmployeeDeductionDto Dto) : IRequest<Result<Guid>>;

public class CreateEmployeeDeductionHandler(IAppDbContext db, ICurrentUser currentUser, IPayrollEngine engine)
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

        if (d.PayrollPeriodId is null)
            return Result<Guid>.Fail("A payroll period is required for a new deduction.");
        var periodExists = await db.PayrollPeriods.AnyAsync(
            p => p.Id == d.PayrollPeriodId && p.CompanyId == d.CompanyId && !p.IsDeleted, ct);
        if (!periodExists) return Result<Guid>.Fail("Payroll period not found in this company.");

        if (d.DeductionTypeId is not null && !await db.DeductionTypes.AnyAsync(
                t => t.Id == d.DeductionTypeId && t.CompanyId == d.CompanyId && t.IsActive && !t.IsDeleted, ct))
            return Result<Guid>.Fail("The selected deduction type is not active for this company.");

        var deduction = new EmployeeDeduction
        {
            CompanyId = d.CompanyId,
            EmployeeId = d.EmployeeId,
            PayrollPeriodId = d.PayrollPeriodId,
            DeductionTypeId = d.DeductionTypeId,
            Description = d.Description.Trim(),
            Category = d.Category,
            EmployeeAmount = d.EmployeeAmount,
            EmployerAmount = d.EmployerAmount,
            IsActive = true
        };
        db.EmployeeDeductions.Add(deduction);
        await db.SaveChangesAsync(ct);

        await EmployeeDeductionRecalculation.ReprocessOpenPeriodsAsync(d.CompanyId, d.EmployeeId, d.PayrollPeriodId, db, engine, ct);
        return Result<Guid>.Ok(deduction.Id);
    }
}

public record UpdateEmployeeDeductionDto(
    string Description, DeductionCategory Category, decimal EmployeeAmount,
    decimal EmployerAmount, bool IsActive, Guid? DeductionTypeId = null, Guid? PayrollPeriodId = null);

public record UpdateEmployeeDeductionCommand(Guid Id, UpdateEmployeeDeductionDto Dto) : IRequest<Result>;

public class UpdateEmployeeDeductionHandler(IAppDbContext db, ICurrentUser currentUser, IPayrollEngine engine)
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
        if (request.Dto.DeductionTypeId is not null && !await db.DeductionTypes.AnyAsync(
                t => t.Id == request.Dto.DeductionTypeId && t.CompanyId == deduction.CompanyId && t.IsActive && !t.IsDeleted, ct))
            return Result.Fail("The selected deduction type is not active for this company.");

        deduction.Description = request.Dto.Description.Trim();
        if (request.Dto.PayrollPeriodId is not null)
        {
            var periodExists = await db.PayrollPeriods.AnyAsync(
                p => p.Id == request.Dto.PayrollPeriodId && p.CompanyId == deduction.CompanyId && !p.IsDeleted, ct);
            if (!periodExists) return Result.Fail("Payroll period not found in this company.");
            deduction.PayrollPeriodId = request.Dto.PayrollPeriodId;
        }
        deduction.DeductionTypeId = request.Dto.DeductionTypeId;
        deduction.Category = request.Dto.Category;
        deduction.EmployeeAmount = request.Dto.EmployeeAmount;
        deduction.EmployerAmount = request.Dto.EmployerAmount;
        deduction.IsActive = request.Dto.IsActive;
        await db.SaveChangesAsync(ct);

        await EmployeeDeductionRecalculation.ReprocessOpenPeriodsAsync(deduction.CompanyId, deduction.EmployeeId, deduction.PayrollPeriodId, db, engine, ct);
        return Result.Ok();
    }
}

internal static class EmployeeDeductionRecalculation
{
    public static async Task ReprocessOpenPeriodsAsync(
        Guid companyId, Guid employeeId, Guid? payrollPeriodId, IAppDbContext db, IPayrollEngine engine, CancellationToken ct)
    {
        var periods = await db.PayrollPeriods
            .Where(p => p.CompanyId == companyId
                && (payrollPeriodId == null || p.Id == payrollPeriodId)
                && (p.Status == PayrollStatus.Draft || p.Status == PayrollStatus.Approved)
                && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var periodId in periods)
            await engine.ProcessLineAsync(periodId, employeeId, ct);
    }
}
