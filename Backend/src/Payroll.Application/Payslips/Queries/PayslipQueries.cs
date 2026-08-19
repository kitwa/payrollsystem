using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Payslips.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Payslips.Queries;

public record GetPayslipsForEmployeeQuery(Guid EmployeeId) : IRequest<Result<List<PayslipListItemDto>>>;

public class GetPayslipsForEmployeeHandler(IAppDbContext db) : IRequestHandler<GetPayslipsForEmployeeQuery, Result<List<PayslipListItemDto>>>
{
    public async Task<Result<List<PayslipListItemDto>>> Handle(GetPayslipsForEmployeeQuery request, CancellationToken ct)
    {
        var result = await db.PayrollLines
            .Include(l => l.PayrollPeriod)
            .Where(l => l.EmployeeId == request.EmployeeId && !l.IsDeleted)
            .OrderByDescending(l => l.PayrollPeriod.Year).ThenByDescending(l => l.PayrollPeriod.Month)
            .Select(l => new PayslipListItemDto(
                l.Id, l.PayrollPeriodId, l.PayrollPeriod.Year, l.PayrollPeriod.Month, l.PayrollPeriod.Status,
                l.GrossEarnings, l.TotalDeductions, l.NetPay))
            .ToListAsync(ct);

        return Result<List<PayslipListItemDto>>.Ok(result);
    }
}

public record GeneratePayslipPdfQuery(Guid PayrollLineId) : IRequest<Result<byte[]>>;

public class GeneratePayslipPdfHandler(IAppDbContext db, IPdfService pdfService) : IRequestHandler<GeneratePayslipPdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GeneratePayslipPdfQuery request, CancellationToken ct)
    {
        var exists = await db.PayrollLines.AnyAsync(l => l.Id == request.PayrollLineId && !l.IsDeleted, ct);
        if (!exists) return Result<byte[]>.Fail("Payslip not found.");

        var bytes = await pdfService.GeneratePayslipAsync(request.PayrollLineId, ct);
        return Result<byte[]>.Ok(bytes);
    }
}
