using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Leave;

namespace Payroll.Application.Leave;

/// <summary>Creates missing LeaveBalance rows from active leave types so balances are always queryable.</summary>
public static class LeaveBalanceMaintenance
{
    public static async Task EnsureForEmployeeAsync(IAppDbContext db, Guid companyId, Guid employeeId, int year, CancellationToken ct)
    {
        var leaveTypes = await GetVisibleLeaveTypesAsync(db, companyId, ct);
        if (leaveTypes.Count == 0) return;

        var existingTypeIds = await db.LeaveBalances
            .Where(b => !b.IsDeleted && b.EmployeeId == employeeId && b.Year == year)
            .Select(b => b.LeaveTypeId)
            .ToListAsync(ct);

        var missing = leaveTypes.Where(t => !existingTypeIds.Contains(t.Id)).ToList();
        if (missing.Count == 0) return;

        foreach (var type in missing)
            db.LeaveBalances.Add(new LeaveBalance
            {
                EmployeeId = employeeId,
                LeaveTypeId = type.Id,
                EntitlementDays = type.DefaultEntitlementDays,
                UsedDays = 0,
                Year = year
            });

        await db.SaveChangesAsync(ct);
    }

    public static async Task EnsureForCompanyAsync(IAppDbContext db, Guid companyId, int year, CancellationToken ct)
    {
        var leaveTypes = await GetVisibleLeaveTypesAsync(db, companyId, ct);
        if (leaveTypes.Count == 0) return;

        var employeeIds = await db.Employees
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync(ct);
        if (employeeIds.Count == 0) return;

        var existing = await db.LeaveBalances
            .Where(b => !b.IsDeleted && b.Year == year && employeeIds.Contains(b.EmployeeId))
            .Select(b => new { b.EmployeeId, b.LeaveTypeId })
            .ToListAsync(ct);
        var existingSet = existing.Select(e => (e.EmployeeId, e.LeaveTypeId)).ToHashSet();

        foreach (var employeeId in employeeIds)
        {
            foreach (var type in leaveTypes)
            {
                if (existingSet.Contains((employeeId, type.Id))) continue;
                db.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = type.Id,
                    EntitlementDays = type.DefaultEntitlementDays,
                    UsedDays = 0,
                    Year = year
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<VisibleLeaveType>> GetVisibleLeaveTypesAsync(IAppDbContext db, Guid companyId, CancellationToken ct) =>
        await db.LeaveTypes
            .Where(t => !t.IsDeleted
                && t.IsActive
                && (t.CompanyId == companyId
                    || (t.CompanyId == Guid.Empty
                        && !db.LeaveTypes.Any(o => !o.IsDeleted && o.CompanyId == companyId && o.Name == t.Name))))
            .Select(t => new VisibleLeaveType(t.Id, t.DefaultEntitlementDays))
            .ToListAsync(ct);

    private record VisibleLeaveType(Guid Id, decimal DefaultEntitlementDays);
}
