using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Settings.DTOs;
using Payroll.Domain.Settings;
using Payroll.Shared;

namespace Payroll.Application.Settings.Commands;

public record CreateLeaveTypeCommand(CreateLeaveTypeDto Dto) : IRequest<Result<Guid>>;

public class CreateLeaveTypeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateLeaveTypeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLeaveTypeCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to create this leave type.");
        var type = new LeaveType
        {
            CompanyId = d.CompanyId,
            Name = d.Name,
            DefaultEntitlementDays = d.DefaultEntitlementDays,
            IsPaid = d.IsPaid,
            RequiresApproval = d.RequiresApproval,
            IsActive = true
        };
        db.LeaveTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(type.Id);
    }
}

public record UpdateLeaveTypeCommand(Guid Id, UpdateLeaveTypeDto Dto) : IRequest<Result>;

public class UpdateLeaveTypeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateLeaveTypeCommand, Result>
{
    public async Task<Result> Handle(UpdateLeaveTypeCommand request, CancellationToken ct)
    {
        var type = await db.LeaveTypes.FindAsync([request.Id], ct);
        if (type is null) return Result.Fail("Leave type not found.");
        var companyId = type.CompanyId == Guid.Empty ? currentUser.CompanyId : type.CompanyId;
        if (companyId is null || !TenantAccess.CanManageCompany(currentUser, companyId.Value))
            return Result.Fail("You are not authorized to update this leave type.");

        var d = request.Dto;
        if (type.CompanyId == Guid.Empty)
        {
            var companyOverride = await db.LeaveTypes.FirstOrDefaultAsync(t =>
                !t.IsDeleted && t.CompanyId == companyId.Value && t.Name == type.Name, ct);
            if (companyOverride is null)
            {
                companyOverride = new LeaveType
                {
                    CompanyId = companyId.Value,
                    Name = d.Name,
                    DefaultEntitlementDays = d.DefaultEntitlementDays,
                    IsPaid = d.IsPaid,
                    RequiresApproval = d.RequiresApproval,
                    IsActive = d.IsActive
                };
                db.LeaveTypes.Add(companyOverride);
            }
            else
            {
                companyOverride.Name = d.Name;
                companyOverride.DefaultEntitlementDays = d.DefaultEntitlementDays;
                companyOverride.IsPaid = d.IsPaid;
                companyOverride.RequiresApproval = d.RequiresApproval;
                companyOverride.IsActive = d.IsActive;
            }

            await db.SaveChangesAsync(ct);
            return Result.Ok();
        }

        type.Name = d.Name;
        type.DefaultEntitlementDays = d.DefaultEntitlementDays;
        type.IsPaid = d.IsPaid;
        type.RequiresApproval = d.RequiresApproval;
        type.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record CreateEarningTypeCommand(CreateEarningTypeDto Dto) : IRequest<Result<Guid>>;

public class CreateEarningTypeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateEarningTypeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEarningTypeCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to create this earning type.");
        var type = new EarningType { CompanyId = d.CompanyId, Name = d.Name, Code = d.Code, IsTaxable = d.IsTaxable, IsActive = true };
        db.EarningTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(type.Id);
    }
}

public record UpdateEarningTypeCommand(Guid Id, UpdateEarningTypeDto Dto) : IRequest<Result>;

public class UpdateEarningTypeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateEarningTypeCommand, Result>
{
    public async Task<Result> Handle(UpdateEarningTypeCommand request, CancellationToken ct)
    {
        var type = await db.EarningTypes.FindAsync([request.Id], ct);
        if (type is null) return Result.Fail("Earning type not found.");
        if (!TenantAccess.CanManageCompany(currentUser, type.CompanyId))
            return Result.Fail("You are not authorized to update this earning type.");

        var d = request.Dto;
        type.Name = d.Name;
        type.Code = d.Code;
        type.IsTaxable = d.IsTaxable;
        type.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record CreateDeductionTypeCommand(CreateDeductionTypeDto Dto) : IRequest<Result<Guid>>;

public class CreateDeductionTypeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateDeductionTypeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDeductionTypeCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to create this deduction type.");
        var type = new DeductionType { CompanyId = d.CompanyId, Name = d.Name, Code = d.Code, IsEmployerContribution = d.IsEmployerContribution, IsActive = true };
        db.DeductionTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(type.Id);
    }
}

public record UpdateDeductionTypeCommand(Guid Id, UpdateDeductionTypeDto Dto) : IRequest<Result>;

public class UpdateDeductionTypeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateDeductionTypeCommand, Result>
{
    public async Task<Result> Handle(UpdateDeductionTypeCommand request, CancellationToken ct)
    {
        var type = await db.DeductionTypes.FindAsync([request.Id], ct);
        if (type is null) return Result.Fail("Deduction type not found.");
        if (!TenantAccess.CanManageCompany(currentUser, type.CompanyId))
            return Result.Fail("You are not authorized to update this deduction type.");

        var d = request.Dto;
        type.Name = d.Name;
        type.Code = d.Code;
        type.IsEmployerContribution = d.IsEmployerContribution;
        type.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
