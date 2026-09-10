namespace Payroll.Application.Common.Interfaces;

/// <summary>Drives the payroll engine — processes all employee lines for a period.</summary>
public interface IPayrollEngine
{
    Task ProcessPeriodAsync(Guid periodId, IReadOnlyCollection<Guid> employeeIds, CancellationToken ct = default);
    Task ProcessLineAsync(Guid periodId, Guid employeeId, CancellationToken ct = default);
}
