using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Leave;
using Payroll.Application.Leave.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Leave.Queries;

public record GetLeaveRequestsQuery(Guid CompanyId, Guid? EmployeeId) : IRequest<Result<List<LeaveRequestDto>>>;

public class GetLeaveRequestsHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLeaveRequestsQuery, Result<List<LeaveRequestDto>>>
{
    public async Task<Result<List<LeaveRequestDto>>> Handle(GetLeaveRequestsQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<LeaveRequestDto>>.Fail("You are not authorized to view this company's leave.");

        var query = db.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Where(l => !l.IsDeleted && l.Employee.CompanyId == request.CompanyId);

        if (request.EmployeeId is not null)
            query = query.Where(l => l.EmployeeId == request.EmployeeId);

        var result = await query
            .OrderByDescending(l => l.StartDate)
            .Select(l => new LeaveRequestDto(
                l.Id, l.EmployeeId, $"{l.Employee.FirstName} {l.Employee.LastName}", l.LeaveType.Name,
                l.StartDate, l.EndDate, l.Days, l.Reason, l.Status, l.ReviewedBy, l.ReviewedAt))
            .ToListAsync(ct);

        return Result<List<LeaveRequestDto>>.Ok(result);
    }
}

public record GetLeaveRequestByIdQuery(Guid Id) : IRequest<Result<LeaveRequestDto>>;

public class GetLeaveRequestByIdHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLeaveRequestByIdQuery, Result<LeaveRequestDto>>
{
    public async Task<Result<LeaveRequestDto>> Handle(GetLeaveRequestByIdQuery request, CancellationToken ct)
    {
        var l = await db.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted, ct);

        if (l is null) return Result<LeaveRequestDto>.Fail("Leave request not found.");
        if (!TenantAccess.CanAccessEmployee(currentUser, l.Employee.CompanyId, l.EmployeeId))
            return Result<LeaveRequestDto>.Fail("You are not authorized to view this leave request.");

        return Result<LeaveRequestDto>.Ok(new LeaveRequestDto(
            l.Id, l.EmployeeId, $"{l.Employee.FirstName} {l.Employee.LastName}", l.LeaveType.Name,
            l.StartDate, l.EndDate, l.Days, l.Reason, l.Status, l.ReviewedBy, l.ReviewedAt));
    }
}

public record GetLeaveBalancesQuery(Guid EmployeeId, int? Year = null) : IRequest<Result<List<LeaveBalanceDto>>>;

public class GetLeaveBalancesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLeaveBalancesQuery, Result<List<LeaveBalanceDto>>>
{
    public async Task<Result<List<LeaveBalanceDto>>> Handle(GetLeaveBalancesQuery request, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (employee is null) return Result<List<LeaveBalanceDto>>.Fail("Employee not found.");
        if (!TenantAccess.CanAccessEmployee(currentUser, employee.CompanyId, employee.Id))
            return Result<List<LeaveBalanceDto>>.Fail("You are not authorized to view these leave balances.");

        var year = request.Year ?? DateTime.UtcNow.Year;
        await LeaveBalanceMaintenance.EnsureForEmployeeAsync(db, employee.CompanyId, employee.Id, year, ct);

        var result = await db.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == request.EmployeeId && b.Year == year && !b.IsDeleted)
            .OrderBy(b => b.LeaveType.Name)
            .Select(b => new LeaveBalanceDto(b.LeaveTypeId, b.LeaveType.Name, b.EntitlementDays, b.UsedDays, b.BalanceDays))
            .ToListAsync(ct);

        return Result<List<LeaveBalanceDto>>.Ok(result);
    }
}

public record GetMyLeaveBalancesQuery(int? Year = null) : IRequest<Result<List<LeaveBalanceDto>>>;

public class GetMyLeaveBalancesHandler(ICurrentUser currentUser, ISender sender)
    : IRequestHandler<GetMyLeaveBalancesQuery, Result<List<LeaveBalanceDto>>>
{
    public async Task<Result<List<LeaveBalanceDto>>> Handle(GetMyLeaveBalancesQuery request, CancellationToken ct)
    {
        if (currentUser.EmployeeId is null)
            return Result<List<LeaveBalanceDto>>.Fail("Your account is not linked to an employee record.");
        return await sender.Send(new GetLeaveBalancesQuery(currentUser.EmployeeId.Value, request.Year), ct);
    }
}

public record GetCompanyLeaveBalancesQuery(Guid CompanyId, int? Year = null) : IRequest<Result<List<EmployeeLeaveBalanceDto>>>;

public class GetCompanyLeaveBalancesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetCompanyLeaveBalancesQuery, Result<List<EmployeeLeaveBalanceDto>>>
{
    public async Task<Result<List<EmployeeLeaveBalanceDto>>> Handle(GetCompanyLeaveBalancesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanManageCompany(currentUser, request.CompanyId))
            return Result<List<EmployeeLeaveBalanceDto>>.Fail("You are not authorized to view leave balances for this company.");

        var year = request.Year ?? DateTime.UtcNow.Year;
        await LeaveBalanceMaintenance.EnsureForCompanyAsync(db, request.CompanyId, year, ct);

        var employees = await db.Employees
            .Where(e => e.CompanyId == request.CompanyId && !e.IsDeleted)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new { e.Id, e.EmployeeNumber, e.FirstName, e.LastName })
            .ToListAsync(ct);

        var balances = await db.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => !b.IsDeleted && b.Year == year && b.Employee.CompanyId == request.CompanyId)
            .OrderBy(b => b.LeaveType.Name)
            .Select(b => new { b.EmployeeId, Balance = new LeaveBalanceDto(b.LeaveTypeId, b.LeaveType.Name, b.EntitlementDays, b.UsedDays, b.BalanceDays) })
            .ToListAsync(ct);

        var balancesByEmployee = balances
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Balance).ToList());

        var result = employees
            .Select(e => new EmployeeLeaveBalanceDto(
                e.Id, e.EmployeeNumber, $"{e.FirstName} {e.LastName}",
                balancesByEmployee.TryGetValue(e.Id, out var value) ? value : []))
            .ToList();

        return Result<List<EmployeeLeaveBalanceDto>>.Ok(result);
    }
}
