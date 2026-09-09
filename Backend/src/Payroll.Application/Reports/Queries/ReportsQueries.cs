using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Reports.DTOs;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Shared;

namespace Payroll.Application.Reports.Queries;

public record GetPayrollRegisterQuery(Guid PeriodId) : IRequest<Result<PayrollRegisterDto>>;

public class GetPayrollRegisterHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetPayrollRegisterQuery, Result<PayrollRegisterDto>>
{
    public async Task<Result<PayrollRegisterDto>> Handle(GetPayrollRegisterQuery request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result<PayrollRegisterDto>.Fail("Payroll period not found.");
        if (!TenantAccess.CanAccessCompany(currentUser, period.CompanyId))
            return Result<PayrollRegisterDto>.Fail("You are not authorized to view this report.");

        var lines = await db.PayrollLines
            .Include(l => l.Employee)
            .Where(l => l.PayrollPeriodId == request.PeriodId && !l.IsDeleted)
            .OrderBy(l => l.Employee.LastName).ThenBy(l => l.Employee.FirstName)
            .Select(l => new PayrollRegisterLineDto(l.Employee.EmployeeNumber, $"{l.Employee.FirstName} {l.Employee.LastName}",
                l.GrossEarnings, l.TotalDeductions, l.NetPay))
            .ToListAsync(ct);

        return Result<PayrollRegisterDto>.Ok(new PayrollRegisterDto(
            period.Id, period.Year, period.Month, lines,
            lines.Sum(l => l.GrossEarnings), lines.Sum(l => l.TotalDeductions), lines.Sum(l => l.NetPay)));
    }
}

public record GetLeaveReportQuery(Guid CompanyId, DateTime From, DateTime To) : IRequest<Result<List<LeaveReportLineDto>>>;

public class GetLeaveReportHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLeaveReportQuery, Result<List<LeaveReportLineDto>>>
{
    public async Task<Result<List<LeaveReportLineDto>>> Handle(GetLeaveReportQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<LeaveReportLineDto>>.Fail("You are not authorized to view this report.");

        var result = await db.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Where(l => !l.IsDeleted && l.Employee.CompanyId == request.CompanyId
                && l.StartDate <= request.To && l.EndDate >= request.From)
            .OrderBy(l => l.StartDate)
            .Select(l => new LeaveReportLineDto($"{l.Employee.FirstName} {l.Employee.LastName}", l.LeaveType.Name,
                l.StartDate, l.EndDate, l.Days, l.Status.ToString()))
            .ToListAsync(ct);

        return Result<List<LeaveReportLineDto>>.Ok(result);
    }
}

public record GetStatutoryReportQuery(Guid PeriodId, DeductionCategory Category) : IRequest<Result<StatutoryReportDto>>;

public class GetStatutoryReportHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetStatutoryReportQuery, Result<StatutoryReportDto>>
{
    public async Task<Result<StatutoryReportDto>> Handle(GetStatutoryReportQuery request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result<StatutoryReportDto>.Fail("Payroll period not found.");
        if (!TenantAccess.CanAccessCompany(currentUser, period.CompanyId))
            return Result<StatutoryReportDto>.Fail("You are not authorized to view this report.");

        var lines = await db.Deductions
            .Include(d => d.PayrollLine).ThenInclude(l => l.Employee)
            .Where(d => d.PayrollLine.PayrollPeriodId == request.PeriodId && d.Category == request.Category && !d.IsDeleted)
            .OrderBy(d => d.PayrollLine.Employee.LastName)
            .Select(d => new StatutoryReportLineDto(d.PayrollLine.Employee.EmployeeNumber,
                $"{d.PayrollLine.Employee.FirstName} {d.PayrollLine.Employee.LastName}", d.EmployeeAmount, d.EmployerAmount))
            .ToListAsync(ct);

        return Result<StatutoryReportDto>.Ok(new StatutoryReportDto(
            period.Id, period.Year, period.Month, lines,
            lines.Sum(l => l.EmployeeAmount), lines.Sum(l => l.EmployerAmount)));
    }
}

public record GetEmployeeCostReportQuery(Guid CompanyId, int Year) : IRequest<Result<EmployeeCostReportDto>>;

public class GetEmployeeCostReportHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetEmployeeCostReportQuery, Result<EmployeeCostReportDto>>
{
    public async Task<Result<EmployeeCostReportDto>> Handle(GetEmployeeCostReportQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<EmployeeCostReportDto>.Fail("You are not authorized to view this report.");

        var lines = await db.PayrollLines
            .Include(l => l.Employee)
            .Include(l => l.Deductions)
            .Where(l => !l.IsDeleted && l.Employee.CompanyId == request.CompanyId && l.PayrollPeriod.Year == request.Year)
            .ToListAsync(ct);

        var grouped = lines
            .GroupBy(l => l.Employee)
            .Select(g => new EmployeeCostReportLineDto(
                g.Key.EmployeeNumber, $"{g.Key.FirstName} {g.Key.LastName}",
                g.Sum(l => l.GrossEarnings),
                g.SelectMany(l => l.Deductions).Sum(d => d.EmployerAmount),
                g.Sum(l => l.GrossEarnings) + g.SelectMany(l => l.Deductions).Sum(d => d.EmployerAmount)))
            .OrderBy(l => l.EmployeeName)
            .ToList();

        return Result<EmployeeCostReportDto>.Ok(new EmployeeCostReportDto(request.Year, grouped, grouped.Sum(l => l.TotalCost)));
    }
}
