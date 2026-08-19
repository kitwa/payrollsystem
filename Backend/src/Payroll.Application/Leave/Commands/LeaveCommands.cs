using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Leave.DTOs;
using Payroll.Domain.Leave;
using Payroll.Domain.Leave.Enums;
using Payroll.Shared;

namespace Payroll.Application.Leave.Commands;

public record RequestLeaveCommand(CreateLeaveRequestDto Dto) : IRequest<Result<Guid>>;

public class RequestLeaveHandler(IAppDbContext db) : IRequestHandler<RequestLeaveCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestLeaveCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (d.EndDate < d.StartDate) return Result<Guid>.Fail("End date cannot be before start date.");

        var balance = await db.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == d.EmployeeId && b.LeaveTypeId == d.LeaveTypeId
                && b.Year == d.StartDate.Year, ct);

        var days = (decimal)(d.EndDate - d.StartDate).TotalDays + 1;

        if (balance is not null && balance.BalanceDays < days)
            return Result<Guid>.Fail("Insufficient leave balance.");

        var leave = new LeaveRequest
        {
            EmployeeId = d.EmployeeId,
            LeaveTypeId = d.LeaveTypeId,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            Days = days,
            Reason = d.Reason,
            Status = LeaveStatus.Pending
        };

        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(leave.Id);
    }
}

public record ApproveLeaveCommand(Guid LeaveRequestId, string ReviewedBy) : IRequest<Result>;

public class ApproveLeaveHandler(IAppDbContext db) : IRequestHandler<ApproveLeaveCommand, Result>
{
    public async Task<Result> Handle(ApproveLeaveCommand request, CancellationToken ct)
    {
        var leave = await db.LeaveRequests.FindAsync([request.LeaveRequestId], ct);
        if (leave is null) return Result.Fail("Leave request not found.");
        if (leave.Status != LeaveStatus.Pending) return Result.Fail("Only pending requests can be approved.");

        leave.Status = LeaveStatus.Approved;
        leave.ReviewedBy = request.ReviewedBy;
        leave.ReviewedAt = DateTime.UtcNow;

        var balance = await db.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId
                && b.LeaveTypeId == leave.LeaveTypeId && b.Year == leave.StartDate.Year, ct);
        if (balance is not null) balance.UsedDays += leave.Days;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record RejectLeaveCommand(Guid LeaveRequestId, string ReviewedBy, string Note) : IRequest<Result>;

public class RejectLeaveHandler(IAppDbContext db) : IRequestHandler<RejectLeaveCommand, Result>
{
    public async Task<Result> Handle(RejectLeaveCommand request, CancellationToken ct)
    {
        var leave = await db.LeaveRequests.FindAsync([request.LeaveRequestId], ct);
        if (leave is null) return Result.Fail("Leave request not found.");
        if (leave.Status != LeaveStatus.Pending) return Result.Fail("Only pending requests can be rejected.");

        leave.Status = LeaveStatus.Rejected;
        leave.ReviewedBy = request.ReviewedBy;
        leave.ReviewedAt = DateTime.UtcNow;
        leave.ReviewNote = request.Note;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
