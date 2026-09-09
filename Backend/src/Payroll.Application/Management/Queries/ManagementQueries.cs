using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Management.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;
using Payroll.Shared;

namespace Payroll.Application.Management.Queries;

public record GetCompanyManagementSummaryQuery(Guid CompanyId) : IRequest<Result<CompanyManagementSummaryDto>>;

public class GetCompanyManagementSummaryHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<GetCompanyManagementSummaryQuery, Result<CompanyManagementSummaryDto>>
{
    public async Task<Result<CompanyManagementSummaryDto>> Handle(GetCompanyManagementSummaryQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result<CompanyManagementSummaryDto>.Fail("Only SuperAdmin users can access management analytics.");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId && !c.IsDeleted, ct);
        if (company is null) return Result<CompanyManagementSummaryDto>.Fail("Company not found.");

        var users = await userManager.Users
            .Where(u => u.CompanyId == request.CompanyId)
            .Select(u => new { u.IsActive })
            .ToListAsync(ct);
        var employees = await db.Employees
            .Where(e => e.CompanyId == request.CompanyId && !e.IsDeleted)
            .Select(e => new { e.Status })
            .ToListAsync(ct);
        var periods = await db.PayrollPeriods
            .Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new { p.Id, p.Year, p.Month })
            .ToListAsync(ct);
        var periodIds = periods.Select(p => p.Id).ToList();
        var payslips = await db.PayrollLines
            .Where(l => periodIds.Contains(l.PayrollPeriodId) && !l.IsDeleted)
            .Select(l => new { l.PayrollPeriodId, l.NetPay, l.PayslipEmailedAt })
            .ToListAsync(ct);
        var lastActivity = await db.AuditLogs
            .Where(a => a.CompanyId == request.CompanyId)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => (DateTime?)a.OccurredAt)
            .FirstOrDefaultAsync(ct);
        var latest = periods.FirstOrDefault();
        var latestPayrollNet = latest is null
            ? 0m
            : payslips.Where(p => p.PayrollPeriodId == latest.Id).Sum(p => p.NetPay);

        return Result<CompanyManagementSummaryDto>.Ok(new CompanyManagementSummaryDto(
            company.Id, company.Name,
            users.Count, users.Count(u => u.IsActive),
            employees.Count, employees.Count(e => (int)e.Status == 0),
            periods.Count, payslips.Count, payslips.Count(p => p.PayslipEmailedAt != null),
            latestPayrollNet,
            latest is null ? null : new DateTime(latest.Year, latest.Month, 1),
            lastActivity));
    }
}

public record GetManagementAuditQuery(Guid? CompanyId, PaginationParams Params) : IRequest<Result<PagedList<ManagementAuditDto>>>;

public class GetManagementAuditHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetManagementAuditQuery, Result<PagedList<ManagementAuditDto>>>
{
    public async Task<Result<PagedList<ManagementAuditDto>>> Handle(GetManagementAuditQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result<PagedList<ManagementAuditDto>>.Fail("Only SuperAdmin users can access audit history.");

        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (request.CompanyId is not null)
            query = query.Where(a => a.CompanyId == request.CompanyId);

        var result = await query
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new ManagementAuditDto(
                a.Id, a.CompanyId, a.UserEmail, a.IpAddress, a.HttpMethod,
                a.Path, a.StatusCode, a.Action, a.OccurredAt))
            .ToListAsync(ct);

        return Result<PagedList<ManagementAuditDto>>.Ok(
            PagedList<ManagementAuditDto>.Create(result, request.Params.PageNumber, request.Params.PageSize));
    }
}
