using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Infrastructure.PayrollEngine.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.PayrollEngine;

/// <summary>Orchestrates all IPayrollCalculator instances for every employee in a period.</summary>
public class PayrollEngine(AppDbContext db, IEnumerable<IPayrollCalculator> calculators)
    : IPayrollEngine
{
    private readonly IReadOnlyList<IPayrollCalculator> _calculators = calculators.OrderBy(c => c.Order).ToList();

    public async Task ProcessPeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await db.PayrollPeriods
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == periodId, ct)
            ?? throw new InvalidOperationException($"PayrollPeriod {periodId} not found.");

        var employees = await db.Employees
            .Where(e => e.CompanyId == period.CompanyId && !e.IsDeleted
                && e.Status == Domain.Employees.Enums.EmploymentStatus.Active)
            .ToListAsync(ct);

        foreach (var employee in employees)
            await ProcessLineAsync(periodId, employee.Id, ct);
    }

    public async Task ProcessLineAsync(Guid periodId, Guid employeeId, CancellationToken ct = default)
    {
        var period = await db.PayrollPeriods.FindAsync([periodId], ct)
            ?? throw new InvalidOperationException($"PayrollPeriod {periodId} not found.");

        var employee = await db.Employees.FindAsync([employeeId], ct)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found.");

        var taxYear = await db.TaxYears
            .Include(t => t.TaxTables)
            .Include(t => t.TaxThresholds)
            .Include(t => t.TaxRebates)
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.Year)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No active tax year found.");

        var unpaidLeaveDays = await CalculateUnpaidLeaveDaysAsync(employeeId, period.PeriodStart, period.PeriodEnd, ct);

        var companyFlags = await db.Companies
            .Where(c => c.Id == period.CompanyId)
            .Select(c => new { c.IsUifEnabled, c.IsSdlEnabled })
            .FirstOrDefaultAsync(ct);

        var context = new PayrollContext
        {
            EmployeeId = employeeId,
            PeriodId = periodId,
            BasicSalary = employee.BasicSalary,
            DateOfBirth = employee.DateOfBirth,
            PeriodStart = period.PeriodStart,
            PeriodEnd = period.PeriodEnd,
            UnpaidLeaveDays = unpaidLeaveDays,
            IsUifEnabled = companyFlags?.IsUifEnabled ?? true,
            IsSdlEnabled = companyFlags?.IsSdlEnabled ?? true,
            TaxYear = taxYear
        };

        var customDeductions = await db.EmployeeDeductions
            .Where(d => d.EmployeeId == employeeId && d.CompanyId == period.CompanyId && d.IsActive && !d.IsDeleted)
            .ToListAsync(ct);

        var bonuses = await db.EmployeeBonuses
            .Where(b => b.EmployeeId == employeeId && b.PayrollPeriodId == periodId && !b.IsDeleted)
            .ToListAsync(ct);

        foreach (var bonus in bonuses)
        {
            context.Earnings.Add(new Earning
            {
                PayrollLineId = periodId, // will be set by engine after line is saved
                Category = EarningCategory.Bonus,
                Description = $"Bonus: {bonus.Description}",
                Amount = bonus.Amount,
                IsTaxable = true
            });
            context.TaxableIncome += bonus.Amount;
        }

        foreach (var calculator in _calculators)
            await calculator.CalculateAsync(context, ct);

        foreach (var deduction in customDeductions)
        {
            context.Deductions.Add(new Deduction
            {
                Category = deduction.Category,
                Description = deduction.Description,
                EmployeeAmount = deduction.EmployeeAmount,
                EmployerAmount = deduction.EmployerAmount
            });
        }

        // Remove existing line if recalculating
        var existing = db.PayrollLines.FirstOrDefault(l => l.PayrollPeriodId == periodId && l.EmployeeId == employeeId);
        if (existing is not null) db.PayrollLines.Remove(existing);

        var line = new PayrollLine
        {
            PayrollPeriodId = periodId,
            EmployeeId = employeeId,
            GrossEarnings = context.Earnings.Sum(e => e.Amount),
            TotalDeductions = context.Deductions.Sum(d => d.EmployeeAmount),
            Earnings = context.Earnings,
            Deductions = context.Deductions
        };
        line.NetPay = line.GrossEarnings - line.TotalDeductions;

        db.PayrollLines.Add(line);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Sums approved unpaid-leave days that overlap the payroll period, clipped to the period boundaries.</summary>
    private async Task<decimal> CalculateUnpaidLeaveDaysAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd, CancellationToken ct)
    {
        var unpaidLeaveRequests = await db.LeaveRequests
            .Include(l => l.LeaveType)
            .Where(l => l.EmployeeId == employeeId && !l.IsDeleted
                && l.Status == Domain.Leave.Enums.LeaveStatus.Approved
                && !l.LeaveType.IsPaid
                && l.StartDate <= periodEnd && l.EndDate >= periodStart)
            .ToListAsync(ct);

        decimal totalDays = 0;
        foreach (var leave in unpaidLeaveRequests)
        {
            var overlapStart = leave.StartDate > periodStart ? leave.StartDate : periodStart;
            var overlapEnd = leave.EndDate < periodEnd ? leave.EndDate : periodEnd;
            if (overlapEnd >= overlapStart)
                totalDays += (decimal)(overlapEnd - overlapStart).TotalDays + 1;
        }

        return totalDays;
    }
}
