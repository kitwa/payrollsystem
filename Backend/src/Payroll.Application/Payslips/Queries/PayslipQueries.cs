using System.IO.Compression;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Payslips.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Payslips.Queries;

public record GetPayslipsForEmployeeQuery(Guid EmployeeId) : IRequest<Result<List<PayslipListItemDto>>>;

public class GetPayslipsForEmployeeHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetPayslipsForEmployeeQuery, Result<List<PayslipListItemDto>>>
{
    public async Task<Result<List<PayslipListItemDto>>> Handle(GetPayslipsForEmployeeQuery request, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (employee is null) return Result<List<PayslipListItemDto>>.Fail("Employee not found.");

        if (!PayslipAccess.CanAccessEmployee(currentUser, employee.Id, employee.CompanyId))
            return Result<List<PayslipListItemDto>>.Fail("You are not authorized to view these payslips.");

        var result = await db.PayrollLines
            .Include(l => l.PayrollPeriod)
            .Include(l => l.Employee)
            .Where(l => l.EmployeeId == request.EmployeeId && !l.IsDeleted)
            .OrderByDescending(l => l.PayrollPeriod.Year).ThenByDescending(l => l.PayrollPeriod.Month)
            .Select(l => new PayslipListItemDto(
                l.Id, l.PayrollPeriodId, l.EmployeeId,
                l.Employee.FirstName + " " + l.Employee.LastName, l.Employee.EmployeeNumber, l.Employee.Email,
                l.PayrollPeriod.Year, l.PayrollPeriod.Month, l.PayrollPeriod.Status,
                l.GrossEarnings, l.TaxableIncome, l.TotalDeductions, l.NetPay, l.PayslipEmailedAt))
            .ToListAsync(ct);

        return Result<List<PayslipListItemDto>>.Ok(result);
    }
}

public record GetMyPayslipsQuery : IRequest<Result<List<PayslipListItemDto>>>;

public class GetMyPayslipsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyPayslipsQuery, Result<List<PayslipListItemDto>>>
{
    public async Task<Result<List<PayslipListItemDto>>> Handle(GetMyPayslipsQuery request, CancellationToken ct)
    {
        if (currentUser.EmployeeId is null)
            return Result<List<PayslipListItemDto>>.Fail("Your account is not linked to an employee record.");

        var result = await db.PayrollLines
            .Include(l => l.PayrollPeriod)
            .Include(l => l.Employee)
            .Where(l => l.EmployeeId == currentUser.EmployeeId && !l.IsDeleted)
            .OrderByDescending(l => l.PayrollPeriod.Year).ThenByDescending(l => l.PayrollPeriod.Month)
            .Select(l => new PayslipListItemDto(
                l.Id, l.PayrollPeriodId, l.EmployeeId,
                l.Employee.FirstName + " " + l.Employee.LastName, l.Employee.EmployeeNumber, l.Employee.Email,
                l.PayrollPeriod.Year, l.PayrollPeriod.Month, l.PayrollPeriod.Status,
                l.GrossEarnings, l.TaxableIncome, l.TotalDeductions, l.NetPay, l.PayslipEmailedAt))
            .ToListAsync(ct);

        return Result<List<PayslipListItemDto>>.Ok(result);
    }
}

public record GetPayslipPeriodsQuery(Guid CompanyId) : IRequest<Result<List<PayslipPeriodSummaryDto>>>;

public class GetPayslipPeriodsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetPayslipPeriodsQuery, Result<List<PayslipPeriodSummaryDto>>>
{
    public async Task<Result<List<PayslipPeriodSummaryDto>>> Handle(GetPayslipPeriodsQuery request, CancellationToken ct)
    {
        var companyId = currentUser.CompanyId ?? request.CompanyId;
        if (!PayslipAccess.CanManageCompany(currentUser, companyId))
            return Result<List<PayslipPeriodSummaryDto>>.Fail("You are not authorized to view these payslips.");

        var result = await db.PayrollPeriods
            .Where(p => p.CompanyId == companyId && !p.IsDeleted)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new PayslipPeriodSummaryDto(
                p.Id, p.Year, p.Month, p.PeriodStart, p.PeriodEnd, p.Status,
                p.Lines.Count(l => !l.IsDeleted),
                p.Lines.Count(l => !l.IsDeleted && l.PayslipEmailedAt != null),
                p.Lines.Where(l => !l.IsDeleted).Sum(l => l.NetPay)))
            .ToListAsync(ct);

        return Result<List<PayslipPeriodSummaryDto>>.Ok(result);
    }
}

public record GetPayslipsForPeriodQuery(Guid PeriodId) : IRequest<Result<List<PayslipListItemDto>>>;

public class GetPayslipsForPeriodHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetPayslipsForPeriodQuery, Result<List<PayslipListItemDto>>>
{
    public async Task<Result<List<PayslipListItemDto>>> Handle(GetPayslipsForPeriodQuery request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result<List<PayslipListItemDto>>.Fail("Payroll period not found.");

        if (!PayslipAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result<List<PayslipListItemDto>>.Fail("You are not authorized to view these payslips.");

        var result = await db.PayrollLines
            .Include(l => l.Employee)
            .Include(l => l.PayrollPeriod)
            .Where(l => l.PayrollPeriodId == request.PeriodId && !l.IsDeleted)
            .OrderBy(l => l.Employee.LastName).ThenBy(l => l.Employee.FirstName)
            .Select(l => new PayslipListItemDto(
                l.Id, l.PayrollPeriodId, l.EmployeeId,
                l.Employee.FirstName + " " + l.Employee.LastName, l.Employee.EmployeeNumber, l.Employee.Email,
                l.PayrollPeriod.Year, l.PayrollPeriod.Month, l.PayrollPeriod.Status,
                l.GrossEarnings, l.TaxableIncome, l.TotalDeductions, l.NetPay, l.PayslipEmailedAt))
            .ToListAsync(ct);

        return Result<List<PayslipListItemDto>>.Ok(result);
    }
}

public record GetPayslipDetailQuery(Guid PayrollLineId) : IRequest<Result<PayslipDetailDto>>;

public class GetPayslipDetailHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetPayslipDetailQuery, Result<PayslipDetailDto>>
{
    public async Task<Result<PayslipDetailDto>> Handle(GetPayslipDetailQuery request, CancellationToken ct)
    {
        var line = await db.PayrollLines
            .Include(l => l.Employee).ThenInclude(e => e.Company)
            .Include(l => l.PayrollPeriod)
            .Include(l => l.Earnings)
            .Include(l => l.Deductions)
            .FirstOrDefaultAsync(l => l.Id == request.PayrollLineId && !l.IsDeleted, ct);

        if (line is null) return Result<PayslipDetailDto>.Fail("Payslip not found.");
        if (!PayslipAccess.CanAccessLine(currentUser, line))
            return Result<PayslipDetailDto>.Fail("You are not authorized to view this payslip.");

        var e = line.Employee;
        var p = line.PayrollPeriod;

        return Result<PayslipDetailDto>.Ok(new PayslipDetailDto(
            line.Id, line.PayrollPeriodId, e.Company?.Name ?? string.Empty,
            $"{e.FirstName} {e.LastName}", e.EmployeeNumber, e.Email, e.JobTitle, e.Department,
            e.TaxNumber, e.UifNumber,
            p.Year, p.Month, p.PeriodStart, p.PeriodEnd, p.Status,
            line.GrossEarnings, line.TaxableIncome, line.TotalDeductions, line.NetPay, line.PayslipEmailedAt,
            [.. line.Earnings.Select(x => new PayslipLineItemDto(x.Category.ToString(), x.Description, x.Amount))],
            [.. line.Deductions
                .Where(x => x.EmployeeAmount > 0)
                .Select(x => new PayslipLineItemDto(x.Category.ToString(), x.Description, x.EmployeeAmount))]));
    }
}

public record GeneratePayslipPdfQuery(Guid PayrollLineId) : IRequest<Result<byte[]>>;

public class GeneratePayslipPdfHandler(IAppDbContext db, IPdfService pdfService, ICurrentUser currentUser)
    : IRequestHandler<GeneratePayslipPdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GeneratePayslipPdfQuery request, CancellationToken ct)
    {
        var line = await db.PayrollLines
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == request.PayrollLineId && !l.IsDeleted, ct);

        if (line is null) return Result<byte[]>.Fail("Payslip not found.");
        if (!PayslipAccess.CanAccessLine(currentUser, line))
            return Result<byte[]>.Fail("You are not authorized to download this payslip.");

        var bytes = await pdfService.GeneratePayslipAsync(request.PayrollLineId, ct);
        return Result<byte[]>.Ok(bytes);
    }
}

public record DownloadPeriodPayslipsQuery(Guid PeriodId) : IRequest<Result<byte[]>>;

public class DownloadPeriodPayslipsHandler(IAppDbContext db, IPdfService pdfService, ICurrentUser currentUser)
    : IRequestHandler<DownloadPeriodPayslipsQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(DownloadPeriodPayslipsQuery request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result<byte[]>.Fail("Payroll period not found.");

        if (!PayslipAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result<byte[]>.Fail("You are not authorized to download these payslips.");

        var lines = await db.PayrollLines
            .Include(l => l.Employee)
            .Where(l => l.PayrollPeriodId == request.PeriodId && !l.IsDeleted)
            .OrderBy(l => l.Employee.LastName)
            .ToListAsync(ct);

        if (lines.Count == 0) return Result<byte[]>.Fail("This payroll period has no payslips.");

        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            foreach (var line in lines)
            {
                var pdf = await pdfService.GeneratePayslipAsync(line.Id, ct);
                var entry = archive.CreateEntry(
                    $"{line.Employee.EmployeeNumber}-{line.Employee.LastName}-{period.Year}-{period.Month:00}.pdf",
                    CompressionLevel.Optimal);

                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(pdf, ct);
            }
        }

        return Result<byte[]>.Ok(buffer.ToArray());
    }
}
