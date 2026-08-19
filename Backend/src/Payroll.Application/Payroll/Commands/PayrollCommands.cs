using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Payroll.DTOs;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Shared;

namespace Payroll.Application.Payroll.Commands;

public record GeneratePayrollCommand(GeneratePayrollDto Dto) : IRequest<Result<Guid>>;

public class GeneratePayrollHandler(IAppDbContext db, IPayrollEngine engine)
    : IRequestHandler<GeneratePayrollCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GeneratePayrollCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        var exists = await db.PayrollPeriods.AnyAsync(
            p => p.CompanyId == d.CompanyId && p.Year == d.Year && p.Month == d.Month, ct);

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

public class ApprovePayrollHandler(IAppDbContext db) : IRequestHandler<ApprovePayrollCommand, Result>
{
    public async Task<Result> Handle(ApprovePayrollCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FindAsync([request.PeriodId], ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!period.CanTransitionTo(PayrollStatus.Approved))
            return Result.Fail($"Cannot approve a payroll period with status {period.Status}.");

        period.Status = PayrollStatus.Approved;
        period.ApprovedAt = DateTime.UtcNow;
        period.ApprovedBy = request.ApprovedBy;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record LockPayrollCommand(Guid PeriodId) : IRequest<Result>;

public class LockPayrollHandler(IAppDbContext db) : IRequestHandler<LockPayrollCommand, Result>
{
    public async Task<Result> Handle(LockPayrollCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FindAsync([request.PeriodId], ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!period.CanTransitionTo(PayrollStatus.Locked))
            return Result.Fail($"Cannot lock a payroll period with status {period.Status}.");

        period.Status = PayrollStatus.Locked;
        period.LockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record MarkPaidCommand(Guid PeriodId) : IRequest<Result>;

public class MarkPaidHandler(IAppDbContext db) : IRequestHandler<MarkPaidCommand, Result>
{
    public async Task<Result> Handle(MarkPaidCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FindAsync([request.PeriodId], ct);
        if (period is null) return Result.Fail("Payroll period not found.");
        if (!period.CanTransitionTo(PayrollStatus.Paid))
            return Result.Fail($"Cannot mark as paid a payroll period with status {period.Status}.");

        period.Status = PayrollStatus.Paid;
        period.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
