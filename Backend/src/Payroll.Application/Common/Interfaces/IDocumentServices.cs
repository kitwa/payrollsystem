namespace Payroll.Application.Common.Interfaces;

public interface IPdfService
{
    Task<byte[]> GeneratePayslipAsync(Guid payrollLineId, CancellationToken ct = default);
}

public interface ITaxCertificatePdfService
{
    Task<byte[]> GenerateAsync(Guid certificateId, CancellationToken ct = default);
}

public interface IExcelService
{
    Task<byte[]> GeneratePayrollRegisterAsync(Guid periodId, CancellationToken ct = default);
    Task<byte[]> GenerateSarsExportAsync(Guid taxYearId, string exportType, CancellationToken ct = default);
}
