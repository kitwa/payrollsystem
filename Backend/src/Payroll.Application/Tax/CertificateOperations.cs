using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Audit;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.Tax;
using Payroll.Shared;

namespace Payroll.Application.Tax;

public record TaxCertificateDto(Guid Id, Guid EmployeeId, string EmployeeName, Guid CompanyId, Guid TaxYearId,
    string TaxYearName, TaxCertificateType CertificateType, string CertificateNumber, TaxCertificateStatus Status,
    decimal GrossRemuneration, decimal TaxableIncome, decimal PAYE, decimal UIF, decimal SDL,
    decimal Allowances, decimal Bonuses, decimal Benefits, DateTime? GeneratedAt, bool IsFinal);

public record TaxCertificateDetailDto(TaxCertificateDto Certificate, List<TaxCertificateLineDto> Lines);
public record TaxCertificateLineDto(Guid Id, string SARSCode, string Description, decimal Amount, decimal TaxableAmount);

public record GetTaxCertificatesQuery(Guid CompanyId, Guid TaxYearId, string? Search, TaxCertificateStatus? Status, TaxCertificateType? Type)
    : IRequest<Result<List<TaxCertificateDto>>>;

public class GetTaxCertificatesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetTaxCertificatesQuery, Result<List<TaxCertificateDto>>>
{
    public async Task<Result<List<TaxCertificateDto>>> Handle(GetTaxCertificatesQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<TaxCertificateDto>>.Fail("You are not authorized to view these certificates.");

        var query = db.EmployeeTaxCertificates.AsNoTracking()
            .Where(c => c.CompanyId == request.CompanyId && c.TaxYearId == request.TaxYearId && !c.IsDeleted);
        if (request.Status is not null) query = query.Where(c => c.Status == request.Status);
        if (request.Type is not null) query = query.Where(c => c.CertificateType == request.Type);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c => (c.Employee.FirstName + " " + c.Employee.LastName).Contains(request.Search)
                || c.Employee.EmployeeNumber.Contains(request.Search));

        var certificates = await query.Include(c => c.Employee).Include(c => c.TaxYear)
            .OrderBy(c => c.Employee.LastName).ThenBy(c => c.Employee.FirstName).ToListAsync(ct);
        var result = certificates.Select(ToDto).ToList();
        return Result<List<TaxCertificateDto>>.Ok(result);
    }

    private static TaxCertificateDto ToDto(EmployeeTaxCertificate c) => new(c.Id, c.EmployeeId,
        (c.Employee is null ? "Unknown employee" : c.Employee.FirstName + " " + c.Employee.LastName), c.CompanyId, c.TaxYearId,
        c.TaxYear?.Name ?? $"Tax year {c.TaxYearId}",
        c.CertificateType, c.CertificateNumber, c.Status, c.GrossRemuneration, c.TaxableIncome,
        c.PAYE, c.UIF, c.SDL, c.Allowances, c.Bonuses, c.Benefits, c.GeneratedAt, c.IsFinal);
}

public record GetTaxCertificateQuery(Guid Id) : IRequest<Result<TaxCertificateDetailDto>>;

public class GetTaxCertificateHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetTaxCertificateQuery, Result<TaxCertificateDetailDto>>
{
    public async Task<Result<TaxCertificateDetailDto>> Handle(GetTaxCertificateQuery request, CancellationToken ct)
    {
        var certificate = await db.EmployeeTaxCertificates.Include(c => c.Employee).Include(c => c.TaxYear)
            .Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);
        if (certificate is null) return Result<TaxCertificateDetailDto>.Fail("Tax certificate not found.");
        if (!TenantAccess.CanAccessCompany(currentUser, certificate.CompanyId))
            return Result<TaxCertificateDetailDto>.Fail("You are not authorized to view this certificate.");
        var dto = new TaxCertificateDto(certificate.Id, certificate.EmployeeId,
            certificate.Employee.FirstName + " " + certificate.Employee.LastName, certificate.CompanyId,
            certificate.TaxYearId, certificate.TaxYear.Name, certificate.CertificateType,
            certificate.CertificateNumber, certificate.Status, certificate.GrossRemuneration,
            certificate.TaxableIncome, certificate.PAYE, certificate.UIF, certificate.SDL,
            certificate.Allowances, certificate.Bonuses, certificate.Benefits, certificate.GeneratedAt, certificate.IsFinal);
        return Result<TaxCertificateDetailDto>.Ok(new(dto, certificate.Lines.Select(l =>
            new TaxCertificateLineDto(l.Id, l.SARSCode, l.Description, l.Amount, l.TaxableAmount)).ToList()));
    }
}

public record GenerateTaxCertificateCommand(Guid CompanyId, Guid EmployeeId, Guid TaxYearId, TaxCertificateType CertificateType = TaxCertificateType.IRP5)
    : IRequest<Result<Guid>>;
public record GenerateAllTaxCertificatesCommand(Guid CompanyId, Guid TaxYearId, TaxCertificateType CertificateType = TaxCertificateType.IRP5)
    : IRequest<Result<int>>;
public record FinalizeTaxCertificateCommand(Guid Id) : IRequest<Result>;

public class GenerateTaxCertificateHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GenerateTaxCertificateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerateTaxCertificateCommand request, CancellationToken ct) =>
        await TaxCertificateGenerator.GenerateAsync(db, currentUser, request.CompanyId, request.EmployeeId, request.TaxYearId, request.CertificateType, ct);
}

public class GenerateAllTaxCertificatesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GenerateAllTaxCertificatesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(GenerateAllTaxCertificatesCommand request, CancellationToken ct)
    {
        if (!TenantAccess.CanManageCompany(currentUser, request.CompanyId))
            return Result<int>.Fail("You are not authorized to generate certificates for this company.");
        var employees = await db.Employees.Where(e => e.CompanyId == request.CompanyId && !e.IsDeleted).Select(e => e.Id).ToListAsync(ct);
        var generated = 0;
        foreach (var employeeId in employees)
        {
            var result = await TaxCertificateGenerator.GenerateAsync(db, currentUser, request.CompanyId, employeeId, request.TaxYearId, request.CertificateType, ct);
            if (!result.IsSuccess) return Result<int>.Fail(result.Errors);
            generated++;
        }
        return Result<int>.Ok(generated);
    }
}

public class FinalizeTaxCertificateHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<FinalizeTaxCertificateCommand, Result>
{
    public async Task<Result> Handle(FinalizeTaxCertificateCommand request, CancellationToken ct)
    {
        var certificate = await db.EmployeeTaxCertificates.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);
        if (certificate is null) return Result.Fail("Tax certificate not found.");
        if (!TenantAccess.CanManageCompany(currentUser, certificate.CompanyId)) return Result.Fail("You are not authorized to finalize this certificate.");
        certificate.IsFinal = true;
        certificate.Status = TaxCertificateStatus.Finalized;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

internal static class TaxCertificateGenerator
{
    public static async Task<Result<Guid>> GenerateAsync(IAppDbContext db, ICurrentUser user, Guid companyId, Guid employeeId, Guid taxYearId, TaxCertificateType type, CancellationToken ct)
    {
        if (!TenantAccess.CanManageCompany(user, companyId)) return Result<Guid>.Fail("You are not authorized to generate this certificate.");
        var taxYear = await db.TaxYears.FirstOrDefaultAsync(y => y.Id == taxYearId && !y.IsDeleted, ct);
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && e.CompanyId == companyId && !e.IsDeleted, ct);
        if (taxYear is null || employee is null) return Result<Guid>.Fail("Tax year or employee not found.");
        var existing = await db.EmployeeTaxCertificates.FirstOrDefaultAsync(c => c.EmployeeId == employeeId && c.TaxYearId == taxYearId && c.CertificateType == type && !c.IsDeleted, ct);
        if (existing?.IsFinal == true) return Result<Guid>.Fail("The finalized certificate cannot be regenerated.");

        var lines = await db.PayrollLines.Include(l => l.PayrollPeriod).Include(l => l.Earnings).Include(l => l.Deductions)
            .Where(l => l.EmployeeId == employeeId && !l.IsDeleted).ToListAsync(ct);
        lines = lines.Where(l => taxYear.ContainsPayrollPeriod(l.PayrollPeriod.PeriodStart, l.PayrollPeriod.PeriodEnd)).ToList();
        var certificate = existing ?? new EmployeeTaxCertificate { EmployeeId = employeeId, CompanyId = companyId, TaxYearId = taxYearId, CertificateType = type };
        certificate.CertificateNumber = existing?.CertificateNumber ?? $"{taxYear.Year}-{employee.EmployeeNumber}-{Guid.NewGuid():N}";
        certificate.GrossRemuneration = lines.Sum(l => l.GrossEarnings);
        certificate.TaxableIncome = lines.Sum(l => l.TaxableIncome);
        certificate.PAYE = lines.SelectMany(l => l.Deductions).Where(d => d.Category == DeductionCategory.Paye).Sum(d => d.EmployeeAmount);
        certificate.UIF = lines.SelectMany(l => l.Deductions).Where(d => d.Category == DeductionCategory.Uif).Sum(d => d.EmployeeAmount);
        certificate.SDL = lines.SelectMany(l => l.Deductions).Where(d => d.Category == DeductionCategory.Sdl).Sum(d => d.EmployerAmount);
        certificate.Allowances = lines.SelectMany(l => l.Earnings).Where(e => e.Category is EarningCategory.Allowance or EarningCategory.TravelAllowance or EarningCategory.CellAllowance).Sum(e => e.Amount);
        certificate.Bonuses = lines.SelectMany(l => l.Earnings).Where(e => e.Category == EarningCategory.Bonus).Sum(e => e.Amount);
        certificate.Benefits = 0;
        certificate.RetirementContributions = lines.SelectMany(l => l.Deductions).Where(d => d.Category == DeductionCategory.RetirementAnnuity).Sum(d => d.EmployeeAmount);
        certificate.ProvidentContributions = lines.SelectMany(l => l.Deductions).Where(d => d.Category == DeductionCategory.ProvidentFund).Sum(d => d.EmployeeAmount);
        certificate.PensionContributions = lines.SelectMany(l => l.Deductions).Where(d => d.Category == DeductionCategory.Pension).Sum(d => d.EmployeeAmount);
        certificate.Status = TaxCertificateStatus.Generated; certificate.GeneratedAt = DateTime.UtcNow; certificate.GeneratedBy = user.UserId;
        if (existing is null) db.EmployeeTaxCertificates.Add(certificate);
        else db.TaxCertificateLines.RemoveRange(await db.TaxCertificateLines.Where(l => l.CertificateId == existing.Id).ToListAsync(ct));
        db.TaxCertificateLines.AddRange(BuildLines(certificate, lines));
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(certificate.Id);
    }
    private static IEnumerable<TaxCertificateLine> BuildLines(EmployeeTaxCertificate c, List<PayrollLine> lines) =>
        new[] { new TaxCertificateLine { CertificateId = c.Id, SARSCode = "3601", Description = "Gross remuneration", Amount = c.GrossRemuneration, TaxableAmount = c.TaxableIncome }, new TaxCertificateLine { CertificateId = c.Id, SARSCode = "4102", Description = "PAYE", Amount = c.PAYE }, new TaxCertificateLine { CertificateId = c.Id, SARSCode = "4141", Description = "UIF", Amount = c.UIF }, new TaxCertificateLine { CertificateId = c.Id, SARSCode = "4150", Description = "SDL", Amount = c.SDL } };
}