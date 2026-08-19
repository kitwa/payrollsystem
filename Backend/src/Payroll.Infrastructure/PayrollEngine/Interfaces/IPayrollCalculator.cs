namespace Payroll.Infrastructure.PayrollEngine.Interfaces;

/// <summary>Every SA payroll calculator implements this — enables ordered, testable execution.</summary>
public interface IPayrollCalculator
{
    /// <summary>Execution order — lower runs first.</summary>
    int Order { get; }

    Task CalculateAsync(PayrollContext context, CancellationToken ct = default);
}
