using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Shared;

namespace Payroll.Application.Payroll;

public record EmployeeBonusDto(
    Guid Id, Guid EmployeeId, Guid PayrollPeriodId, string Description, decimal Amount, string? Notes, Guid? EarningTypeId);

public record GetEmployeeBonusesQuery(Guid CompanyId, Guid PayrollPeriodId, Guid? EmployeeId)
    : IRequest<Result<List<EmployeeBonusDto>>>;

public class GetEmployeeBonusesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeeBonusesQuery, Result<List<EmployeeBonusDto>>>
{
    public async Task<Result<List<EmployeeBonusDto>>> Handle(GetEmployeeBonusesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanManageCompany(currentUser, request.CompanyId))
            return Result<List<EmployeeBonusDto>>.Fail("You are not authorized to view bonuses.");

        var query = db.EmployeeBonuses
            .Where(b => b.CompanyId == request.CompanyId && b.PayrollPeriodId == request.PayrollPeriodId && !b.IsDeleted);

        if (request.EmployeeId is not null)
            query = query.Where(b => b.EmployeeId == request.EmployeeId);

        var result = await query
            .OrderBy(b => b.Description)
            .Select(b => new EmployeeBonusDto(b.Id, b.EmployeeId, b.PayrollPeriodId, b.Description, b.Amount, b.Notes, b.EarningTypeId))
            .ToListAsync(ct);

        return Result<List<EmployeeBonusDto>>.Ok(result);
    }
}

public record CreateEmployeeBonusDto(
    Guid CompanyId, Guid EmployeeId, Guid PayrollPeriodId, string Description, decimal Amount, string? Notes, Guid? EarningTypeId = null);

public record CreateEmployeeBonusCommand(CreateEmployeeBonusDto Dto) : IRequest<Result<Guid>>;

public class CreateEmployeeBonusHandler(IAppDbContext db, ICurrentUser currentUser, IPayrollEngine engine)
    : IRequestHandler<CreateEmployeeBonusCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeBonusCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to add bonuses.");
        if (string.IsNullOrWhiteSpace(d.Description))
            return Result<Guid>.Fail("Bonus description is required.");
        if (d.Amount <= 0)
            return Result<Guid>.Fail("Bonus amount must be greater than zero.");

        var employeeExists = await db.Employees.AnyAsync(
            e => e.Id == d.EmployeeId && e.CompanyId == d.CompanyId && !e.IsDeleted, ct);
        if (!employeeExists) return Result<Guid>.Fail("Employee not found in this company.");

        if (d.EarningTypeId is not null && !await db.EarningTypes.AnyAsync(
                t => t.Id == d.EarningTypeId && t.CompanyId == d.CompanyId && t.IsActive && !t.IsDeleted, ct))
            return Result<Guid>.Fail("The selected earning type is not active for this company.");

        var period = await db.PayrollPeriods.FirstOrDefaultAsync(
            p => p.Id == d.PayrollPeriodId && p.CompanyId == d.CompanyId && !p.IsDeleted, ct);
        if (period is null) return Result<Guid>.Fail("Payroll period not found.");
        if (period.Status is PayrollStatus.Locked or PayrollStatus.Paid)
            return Result<Guid>.Fail("Bonuses cannot be changed once the payroll period is locked or paid.");

        var bonus = new EmployeeBonus
        {
            CompanyId = d.CompanyId,
            EmployeeId = d.EmployeeId,
            EarningTypeId = d.EarningTypeId,
            PayrollPeriodId = d.PayrollPeriodId,
            Description = d.Description.Trim(),
            Amount = d.Amount,
            Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim()
        };
        db.EmployeeBonuses.Add(bonus);
        await db.SaveChangesAsync(ct);

        await engine.ProcessLineAsync(d.PayrollPeriodId, d.EmployeeId, ct);

        return Result<Guid>.Ok(bonus.Id);
    }
}

public record UpdateEmployeeBonusDto(string Description, decimal Amount, string? Notes, Guid? EarningTypeId = null);

public record UpdateEmployeeBonusCommand(Guid Id, UpdateEmployeeBonusDto Dto) : IRequest<Result>;

public class UpdateEmployeeBonusHandler(IAppDbContext db, ICurrentUser currentUser, IPayrollEngine engine)
    : IRequestHandler<UpdateEmployeeBonusCommand, Result>
{
    public async Task<Result> Handle(UpdateEmployeeBonusCommand request, CancellationToken ct)
    {
        var bonus = await db.EmployeeBonuses.FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, ct);
        if (bonus is null) return Result.Fail("Bonus not found.");
        if (!TenantAccess.CanManageCompany(currentUser, bonus.CompanyId))
            return Result.Fail("You are not authorized to update this bonus.");
        if (string.IsNullOrWhiteSpace(request.Dto.Description))
            return Result.Fail("Bonus description is required.");
        if (request.Dto.Amount <= 0)
            return Result.Fail("Bonus amount must be greater than zero.");
        if (request.Dto.EarningTypeId is not null && !await db.EarningTypes.AnyAsync(
                t => t.Id == request.Dto.EarningTypeId && t.CompanyId == bonus.CompanyId && t.IsActive && !t.IsDeleted, ct))
            return Result.Fail("The selected earning type is not active for this company.");

        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == bonus.PayrollPeriodId, ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (period.Status is PayrollStatus.Locked or PayrollStatus.Paid)
            return Result.Fail("Bonuses cannot be changed once the payroll period is locked or paid.");

        bonus.Description = request.Dto.Description.Trim();
        bonus.EarningTypeId = request.Dto.EarningTypeId;
        bonus.Amount = request.Dto.Amount;
        bonus.Notes = string.IsNullOrWhiteSpace(request.Dto.Notes) ? null : request.Dto.Notes.Trim();
        await db.SaveChangesAsync(ct);

        await engine.ProcessLineAsync(bonus.PayrollPeriodId, bonus.EmployeeId, ct);

        return Result.Ok();
    }
}

public record DeleteEmployeeBonusCommand(Guid Id) : IRequest<Result>;

public class DeleteEmployeeBonusHandler(IAppDbContext db, ICurrentUser currentUser, IPayrollEngine engine)
    : IRequestHandler<DeleteEmployeeBonusCommand, Result>
{
    public async Task<Result> Handle(DeleteEmployeeBonusCommand request, CancellationToken ct)
    {
        var bonus = await db.EmployeeBonuses.FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, ct);
        if (bonus is null) return Result.Fail("Bonus not found.");
        if (!TenantAccess.CanManageCompany(currentUser, bonus.CompanyId))
            return Result.Fail("You are not authorized to delete this bonus.");

        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == bonus.PayrollPeriodId, ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (period.Status is PayrollStatus.Locked or PayrollStatus.Paid)
            return Result.Fail("Bonuses cannot be changed once the payroll period is locked or paid.");

        bonus.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await engine.ProcessLineAsync(bonus.PayrollPeriodId, bonus.EmployeeId, ct);

        return Result.Ok();
    }
}
