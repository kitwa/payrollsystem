using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Settings.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Settings.Queries;

public record GetLeaveTypesQuery(Guid CompanyId) : IRequest<Result<List<LeaveTypeDto>>>;

public class GetLeaveTypesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLeaveTypesQuery, Result<List<LeaveTypeDto>>>
{
    public async Task<Result<List<LeaveTypeDto>>> Handle(GetLeaveTypesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<LeaveTypeDto>>.Fail("You are not authorized to view these leave types.");

        var types = await db.LeaveTypes
            .Where(t => !t.IsDeleted && (t.CompanyId == request.CompanyId || t.CompanyId == Guid.Empty) && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new LeaveTypeDto(t.Id, t.CompanyId, t.Name, t.DefaultEntitlementDays, t.IsPaid, t.RequiresApproval, t.IsActive))
            .ToListAsync(ct);

        return Result<List<LeaveTypeDto>>.Ok(types);
    }
}

public record GetEarningTypesQuery(Guid CompanyId) : IRequest<Result<List<EarningTypeDto>>>;

public class GetEarningTypesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetEarningTypesQuery, Result<List<EarningTypeDto>>>
{
    public async Task<Result<List<EarningTypeDto>>> Handle(GetEarningTypesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<EarningTypeDto>>.Fail("You are not authorized to view these earning types.");

        var types = await db.EarningTypes
            .Where(t => !t.IsDeleted && t.CompanyId == request.CompanyId)
            .OrderBy(t => t.Name)
            .Select(t => new EarningTypeDto(t.Id, t.CompanyId, t.Name, t.Code, t.IsTaxable, t.IsActive))
            .ToListAsync(ct);

        return Result<List<EarningTypeDto>>.Ok(types);
    }
}

public record GetDeductionTypesQuery(Guid CompanyId) : IRequest<Result<List<DeductionTypeDto>>>;

public class GetDeductionTypesHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetDeductionTypesQuery, Result<List<DeductionTypeDto>>>
{
    public async Task<Result<List<DeductionTypeDto>>> Handle(GetDeductionTypesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<DeductionTypeDto>>.Fail("You are not authorized to view these deduction types.");

        var types = await db.DeductionTypes
            .Where(t => !t.IsDeleted && t.CompanyId == request.CompanyId)
            .OrderBy(t => t.Name)
            .Select(t => new DeductionTypeDto(t.Id, t.CompanyId, t.Name, t.Code, t.IsEmployerContribution, t.IsActive))
            .ToListAsync(ct);

        return Result<List<DeductionTypeDto>>.Ok(types);
    }
}
