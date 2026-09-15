using System.Net;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Leave.DTOs;
using Payroll.Domain.Identity;
using Payroll.Domain.Leave;
using Payroll.Domain.Leave.Enums;
using Payroll.Shared;

namespace Payroll.Application.Leave.Commands;

public record RequestLeaveCommand(CreateLeaveRequestDto Dto) : IRequest<Result<Guid>>;

public class RequestLeaveHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    UserManager<AppUser> userManager,
    IEmailService emailService,
    ILogger<RequestLeaveHandler> logger) : IRequestHandler<RequestLeaveCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestLeaveCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == d.EmployeeId && !e.IsDeleted, ct);
        if (employee is null) return Result<Guid>.Fail("Employee not found.");
        if (!TenantAccess.CanAccessEmployee(currentUser, employee.CompanyId, employee.Id))
            return Result<Guid>.Fail("You are not authorized to request leave for this employee.");

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

        await NotifyManagersAsync(employee, leave, ct);
        return Result<Guid>.Ok(leave.Id);
    }

    private async Task NotifyManagersAsync(Domain.Employees.Employee employee, LeaveRequest leave, CancellationToken ct)
    {
        try
        {
            var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == leave.LeaveTypeId, ct);
            var admins = await userManager.GetUsersInRoleAsync(Constants.Roles.Admin);
            var payrollManagers = await userManager.GetUsersInRoleAsync(Constants.Roles.PayrollManager);
            var recipients = admins.Concat(payrollManagers)
                .Where(u => u.IsActive && u.CompanyId == employee.CompanyId && !string.IsNullOrWhiteSpace(u.Email))
                .DistinctBy(u => u.Email);

            var body = $"""
                <p>{WebUtility.HtmlEncode(employee.FirstName)} {WebUtility.HtmlEncode(employee.LastName)} has submitted a leave request.</p>
                <p><strong>Leave Type:</strong> {WebUtility.HtmlEncode(leaveType?.Name ?? "Leave")}<br/>
                <strong>Dates:</strong> {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd} ({leave.Days} day(s))<br/>
                <strong>Reason:</strong> {WebUtility.HtmlEncode(leave.Reason ?? "\u2014")}</p>
                """;

            foreach (var recipient in recipients)
                await emailService.SendAsync(recipient.Email!, "New Leave Request Submitted", body, EmailSenderType.Info, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Leave request notification failed for leave request {LeaveRequestId}.", leave.Id);
        }
    }
}

public record ApproveLeaveCommand(Guid LeaveRequestId, string ReviewedBy) : IRequest<Result>;

public class ApproveLeaveHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IEmailService emailService,
    ILogger<ApproveLeaveHandler> logger) : IRequestHandler<ApproveLeaveCommand, Result>
{
    public async Task<Result> Handle(ApproveLeaveCommand request, CancellationToken ct)
    {
        var leave = await db.LeaveRequests.FindAsync([request.LeaveRequestId], ct);
        if (leave is null) return Result.Fail("Leave request not found.");
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == leave.EmployeeId && !e.IsDeleted, ct);
        if (employee is null || !TenantAccess.CanManageCompany(currentUser, employee.CompanyId))
            return Result.Fail("You are not authorized to approve this leave request.");
        if (leave.Status != LeaveStatus.Pending) return Result.Fail("Only pending requests can be approved.");

        leave.Status = LeaveStatus.Approved;
        leave.ReviewedBy = request.ReviewedBy;
        leave.ReviewedAt = DateTime.UtcNow;

        var balance = await db.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId
                && b.LeaveTypeId == leave.LeaveTypeId && b.Year == leave.StartDate.Year, ct);
        if (balance is not null) balance.UsedDays += leave.Days;

        await db.SaveChangesAsync(ct);
        await LeaveDecisionNotifier.SendAsync(db, emailService, logger, employee, leave, "approved", ct);
        return Result.Ok();
    }
}

public record RejectLeaveCommand(Guid LeaveRequestId, string ReviewedBy, string Note) : IRequest<Result>;

public class RejectLeaveHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IEmailService emailService,
    ILogger<RejectLeaveHandler> logger) : IRequestHandler<RejectLeaveCommand, Result>
{
    public async Task<Result> Handle(RejectLeaveCommand request, CancellationToken ct)
    {
        var leave = await db.LeaveRequests.FindAsync([request.LeaveRequestId], ct);
        if (leave is null) return Result.Fail("Leave request not found.");
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == leave.EmployeeId && !e.IsDeleted, ct);
        if (employee is null || !TenantAccess.CanManageCompany(currentUser, employee.CompanyId))
            return Result.Fail("You are not authorized to reject this leave request.");
        if (leave.Status != LeaveStatus.Pending) return Result.Fail("Only pending requests can be rejected.");

        leave.Status = LeaveStatus.Rejected;
        leave.ReviewedBy = request.ReviewedBy;
        leave.ReviewedAt = DateTime.UtcNow;
        leave.ReviewNote = request.Note;
        await db.SaveChangesAsync(ct);
        await LeaveDecisionNotifier.SendAsync(db, emailService, logger, employee, leave, "rejected", ct);
        return Result.Ok();
    }
}

/// <summary>Shared leave decision email, used by both the approve and reject flows.</summary>
file static class LeaveDecisionNotifier
{
    public static async Task SendAsync(
        IAppDbContext db, IEmailService emailService, ILogger logger,
        Domain.Employees.Employee employee, LeaveRequest leave, string decision, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(employee.Email)) return;
        try
        {
            var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == leave.LeaveTypeId, ct);
            var body = $"""
                <p>Hi {WebUtility.HtmlEncode(employee.FirstName)},</p>
                <p>Your leave request has been <strong>{decision}</strong>.</p>
                <p><strong>Leave Type:</strong> {WebUtility.HtmlEncode(leaveType?.Name ?? "Leave")}<br/>
                <strong>Dates:</strong> {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd} ({leave.Days} day(s))</p>
                {(string.IsNullOrWhiteSpace(leave.ReviewNote) ? "" : $"<p><strong>Note:</strong> {WebUtility.HtmlEncode(leave.ReviewNote)}</p>")}
                """;
            await emailService.SendAsync(employee.Email, $"Your Leave Request Has Been {char.ToUpper(decision[0])}{decision[1..]}", body, EmailSenderType.Info, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Leave decision notification failed for leave request {LeaveRequestId}.", leave.Id);
        }
    }
}
