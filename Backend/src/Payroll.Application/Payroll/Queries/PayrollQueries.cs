using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Payroll.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Payroll.Queries;

public record GetPayrollPeriodsQuery(Guid CompanyId) : IRequest<Result<List<PayrollPeriodDto>>>;

public class GetPayrollPeriodsHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetPayrollPeriodsQuery, Result<List<PayrollPeriodDto>>>
{
    public async Task<Result<List<PayrollPeriodDto>>> Handle(GetPayrollPeriodsQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<PayrollPeriodDto>>.Fail("You are not authorized to view this company's payroll.");
        var periods = await db.PayrollPeriods
            .Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted)
            .Include(p => p.Lines)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new PayrollPeriodDto(
                p.Id, p.CompanyId, p.Year, p.Month, p.PeriodStart, p.PeriodEnd, p.Status,
                p.Lines.Count,
                p.Lines.Sum(l => l.GrossEarnings),
                p.Lines.Sum(l => l.TotalDeductions),
                p.Lines.Sum(l => l.NetPay)))
            .ToListAsync(ct);

        return Result<List<PayrollPeriodDto>>.Ok(periods);
    }
}

public record GetPayrollPeriodByIdQuery(Guid PeriodId) : IRequest<Result<PayrollPeriodDto>>;

public class GetPayrollPeriodByIdHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetPayrollPeriodByIdQuery, Result<PayrollPeriodDto>>
{
    public async Task<Result<PayrollPeriodDto>> Handle(GetPayrollPeriodByIdQuery request, CancellationToken ct)
    {
        var p = await db.PayrollPeriods
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);

        if (p is null) return Result<PayrollPeriodDto>.Fail("Payroll period not found.");
        if (!TenantAccess.CanAccessCompany(currentUser, p.CompanyId))
            return Result<PayrollPeriodDto>.Fail("You are not authorized to view this payroll.");

        return Result<PayrollPeriodDto>.Ok(new PayrollPeriodDto(
            p.Id, p.CompanyId, p.Year, p.Month, p.PeriodStart, p.PeriodEnd, p.Status,
            p.Lines.Count,
            p.Lines.Sum(l => l.GrossEarnings),
            p.Lines.Sum(l => l.TotalDeductions),
            p.Lines.Sum(l => l.NetPay)));
    }
}

public record GetPayrollPeriodLinesQuery(Guid PeriodId) : IRequest<Result<List<PayrollLineDto>>>;

public class GetPayrollPeriodLinesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetPayrollPeriodLinesQuery, Result<List<PayrollLineDto>>>
{
    public async Task<Result<List<PayrollLineDto>>> Handle(GetPayrollPeriodLinesQuery request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result<List<PayrollLineDto>>.Fail("Payroll period not found.");
        if (!TenantAccess.CanAccessCompany(currentUser, period.CompanyId))
            return Result<List<PayrollLineDto>>.Fail("You are not authorized to view this payroll.");

        var lines = await db.PayrollLines
            .Include(l => l.Employee)
            .Include(l => l.Earnings)
            .Include(l => l.Deductions)
            .Where(l => l.PayrollPeriodId == request.PeriodId && !l.IsDeleted)
            .OrderBy(l => l.Employee.LastName).ThenBy(l => l.Employee.FirstName)
            .ToListAsync(ct);

        var result = lines.Select(l => new PayrollLineDto(
            l.Id, l.EmployeeId, $"{l.Employee.FirstName} {l.Employee.LastName}", l.Employee.EmployeeNumber,
            l.GrossEarnings, l.TotalDeductions, l.NetPay,
            l.Earnings.Select(e => new EarningDto(e.Category, e.Description, e.Amount)).ToList(),
            l.Deductions.Select(d => new DeductionDto(d.Category, d.Description, d.EmployeeAmount, d.EmployerAmount)).ToList()
        )).ToList();

        return Result<List<PayrollLineDto>>.Ok(result);
    }
}
