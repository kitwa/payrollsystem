using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Payroll.DTOs;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Shared;

namespace Payroll.Application.Payroll.Commands;

public record GeneratePayrollCommand(GeneratePayrollDto Dto) : IRequest<Result<Guid>>;

public class GeneratePayrollHandler(IAppDbContext db, IPayrollEngine engine, ICurrentUser currentUser)
    : IRequestHandler<GeneratePayrollCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GeneratePayrollCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to generate payroll for this company.");

        var exists = await db.PayrollPeriods.AnyAsync(
            p => p.CompanyId == d.CompanyId && p.Year == d.Year && p.Month == d.Month && !p.IsDeleted, ct);

        if (exists) return Result<Guid>.Fail("A payroll period already exists for this month.");

        var start = new DateTime(d.Year, d.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var period = new PayrollPeriod
        {
            CompanyId = d.CompanyId,
            Year = d.Year,
            Month = d.Month,
            PeriodStart = start,
            PeriodEnd = end,
            Status = PayrollStatus.Draft
        };

        db.PayrollPeriods.Add(period);
        await db.SaveChangesAsync(ct);

        await engine.ProcessPeriodAsync(period.Id, ct);
        return Result<Guid>.Ok(period.Id);
    }
}

public record ApprovePayrollCommand(Guid PeriodId, string ApprovedBy) : IRequest<Result>;

public class ApprovePayrollHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<ApprovePayrollCommand, Result>
{
    public async Task<Result> Handle(ApprovePayrollCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FindAsync([request.PeriodId], ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!TenantAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result.Fail("You are not authorized to approve this payroll.");
        if (!period.CanTransitionTo(PayrollStatus.Approved))
            return Result.Fail(period.Status switch
            {
                PayrollStatus.Approved => "This payroll period has already been approved.",
                PayrollStatus.Locked => "This payroll period is locked and cannot be approved again.",
                PayrollStatus.Paid => "This payroll period has already been paid and cannot be approved again.",
                _ => $"Cannot approve a payroll period with status {period.Status}."
            });

        period.Status = PayrollStatus.Approved;
        period.ApprovedAt = DateTime.UtcNow;
        period.ApprovedBy = request.ApprovedBy;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record LockPayrollCommand(Guid PeriodId) : IRequest<Result>;

public class LockPayrollHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<LockPayrollCommand, Result>
{
    public async Task<Result> Handle(LockPayrollCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FindAsync([request.PeriodId], ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!TenantAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result.Fail("You are not authorized to lock this payroll.");
        if (!period.CanTransitionTo(PayrollStatus.Locked))
            return Result.Fail(period.Status switch
            {
                PayrollStatus.Draft => "Approve the payroll period before locking it.",
                PayrollStatus.Locked => "This payroll period has already been locked.",
                PayrollStatus.Paid => "This payroll period has already been paid and is locked.",
                _ => $"Cannot lock a payroll period with status {period.Status}."
            });

        period.Status = PayrollStatus.Locked;
        period.LockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record MarkPaidCommand(Guid PeriodId) : IRequest<Result>;

public class MarkPaidHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<MarkPaidCommand, Result>
{
    public async Task<Result> Handle(MarkPaidCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FindAsync([request.PeriodId], ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!TenantAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result.Fail("You are not authorized to mark this payroll as paid.");
        if (!period.CanTransitionTo(PayrollStatus.Paid))
            return Result.Fail(period.Status switch
            {
                PayrollStatus.Draft => "Approve and lock the payroll period before marking it as paid.",
                PayrollStatus.Approved => "Lock the payroll period before marking it as paid.",
                PayrollStatus.Paid => "This payroll period has already been marked as paid.",
                _ => $"Cannot mark as paid a payroll period with status {period.Status}."
            });

        period.Status = PayrollStatus.Paid;
        period.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record DeletePayrollCommand(Guid PeriodId) : IRequest<Result>;

public class DeletePayrollHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<DeletePayrollCommand, Result>
{
    public async Task<Result> Handle(DeletePayrollCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!TenantAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result.Fail("You are not authorized to delete this payroll period.");
        if (period.Status != PayrollStatus.Draft)
            return Result.Fail("Only a draft payroll period can be deleted. Approved, locked, or paid periods must be kept for audit purposes.");

        var now = DateTime.UtcNow;
        foreach (var line in period.Lines)
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
        }
        period.IsDeleted = true;
        period.DeletedAt = now;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

