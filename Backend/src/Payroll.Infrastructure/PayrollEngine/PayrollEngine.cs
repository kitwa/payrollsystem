using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.PayrollRuns;
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

        var context = new PayrollContext
        {
            EmployeeId = employeeId,
            PeriodId = periodId,
            BasicSalary = employee.BasicSalary,
            DateOfBirth = employee.DateOfBirth,
            TaxYear = taxYear
        };

        foreach (var calculator in _calculators)
            await calculator.CalculateAsync(context, ct);

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
}
