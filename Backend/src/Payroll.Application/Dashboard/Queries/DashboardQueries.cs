using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Dashboard.DTOs;
using Payroll.Domain.Employees.Enums;
using Payroll.Domain.Leave.Enums;
using Payroll.Shared;

namespace Payroll.Application.Dashboard.Queries;

public record GetDashboardSummaryQuery(Guid CompanyId) : IRequest<Result<DashboardSummaryDto>>;

public class GetDashboardSummaryHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<DashboardSummaryDto>.Fail("You are not authorized to view this company's dashboard.");

        var employees = db.Employees.Where(e => e.CompanyId == request.CompanyId && !e.IsDeleted);

        var totalEmployees = await employees.CountAsync(ct);
        var activeEmployees = await employees.CountAsync(e => e.Status == EmploymentStatus.Active, ct);
        var onLeaveEmployees = await employees.CountAsync(e => e.Status == EmploymentStatus.OnLeave, ct);

        var pendingLeave = await db.LeaveRequests
            .CountAsync(l => !l.IsDeleted && l.Status == LeaveStatus.Pending && l.Employee.CompanyId == request.CompanyId, ct);

        var currentYear = DateTime.UtcNow.Year;
        var currentPeriod = await db.PayrollPeriods
            .Include(p => p.Lines)
            .Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(ct);

        var ytdPeriods = await db.PayrollPeriods
            .Include(p => p.Lines)
            .Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted && p.Year == currentYear)
            .ToListAsync(ct);

        var ytdGross = ytdPeriods.SelectMany(p => p.Lines).Sum(l => l.GrossEarnings);
        var ytdDeductions = ytdPeriods.SelectMany(p => p.Lines).Sum(l => l.TotalDeductions);
        var ytdNet = ytdPeriods.SelectMany(p => p.Lines).Sum(l => l.NetPay);

        return Result<DashboardSummaryDto>.Ok(new DashboardSummaryDto(
            totalEmployees, activeEmployees, onLeaveEmployees, pendingLeave,
            currentPeriod?.Id, currentPeriod?.Status.ToString(),
            currentPeriod?.Lines.Count ?? 0,
            currentPeriod?.Lines.Sum(l => l.NetPay) ?? 0,
            ytdGross, ytdDeductions, ytdNet));
    }
}
