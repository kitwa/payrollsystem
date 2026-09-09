using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
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

public record GetLeaveBalancesQuery(Guid EmployeeId) : IRequest<Result<List<LeaveBalanceDto>>>;

public class GetLeaveBalancesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLeaveBalancesQuery, Result<List<LeaveBalanceDto>>>
{
    public async Task<Result<List<LeaveBalanceDto>>> Handle(GetLeaveBalancesQuery request, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (employee is null) return Result<List<LeaveBalanceDto>>.Fail("Employee not found.");
        if (!TenantAccess.CanAccessEmployee(currentUser, employee.CompanyId, employee.Id))
            return Result<List<LeaveBalanceDto>>.Fail("You are not authorized to view these leave balances.");

        var year = DateTime.UtcNow.Year;
        var result = await db.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == request.EmployeeId && b.Year == year && !b.IsDeleted)
            .Select(b => new LeaveBalanceDto(b.LeaveTypeId, b.LeaveType.Name, b.EntitlementDays, b.UsedDays, b.BalanceDays))
            .ToListAsync(ct);

        return Result<List<LeaveBalanceDto>>.Ok(result);
    }
}
