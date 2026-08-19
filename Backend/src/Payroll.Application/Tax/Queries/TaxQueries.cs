using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Tax.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Tax.Queries;

public record GetTaxYearsQuery : IRequest<Result<List<TaxYearSummaryDto>>>;

public class GetTaxYearsHandler(IAppDbContext db) : IRequestHandler<GetTaxYearsQuery, Result<List<TaxYearSummaryDto>>>
{
    public async Task<Result<List<TaxYearSummaryDto>>> Handle(GetTaxYearsQuery request, CancellationToken ct)
    {
        var years = await db.TaxYears
            .Where(y => !y.IsDeleted)
            .OrderByDescending(y => y.Year)
            .Select(y => new TaxYearSummaryDto(y.Id, y.Year, y.StartDate, y.EndDate, y.IsActive))
            .ToListAsync(ct);

        return Result<List<TaxYearSummaryDto>>.Ok(years);
    }
}

public record GetTaxYearDetailQuery(Guid? TaxYearId) : IRequest<Result<TaxYearDetailDto>>;

public class GetTaxYearDetailHandler(IAppDbContext db) : IRequestHandler<GetTaxYearDetailQuery, Result<TaxYearDetailDto>>
{
    public async Task<Result<TaxYearDetailDto>> Handle(GetTaxYearDetailQuery request, CancellationToken ct)
    {
        var query = db.TaxYears
            .Include(y => y.TaxTables)
            .Include(y => y.TaxThresholds)
            .Include(y => y.TaxRebates)
            .Where(y => !y.IsDeleted);

        var year = request.TaxYearId is not null
            ? await query.FirstOrDefaultAsync(y => y.Id == request.TaxYearId, ct)
            : await query.FirstOrDefaultAsync(y => y.IsActive, ct);

        if (year is null) return Result<TaxYearDetailDto>.Fail("Tax year not found.");

        return Result<TaxYearDetailDto>.Ok(new TaxYearDetailDto(
            year.Id, year.Year, year.StartDate, year.EndDate,
            year.UifMonthlyEarningsCeiling, year.UifContributionRate, year.SdlRate, year.IsActive,
            year.TaxTables.OrderBy(t => t.IncomeFrom).Select(t => new TaxTableDto(t.Id, t.IncomeFrom, t.IncomeTo, t.BaseTax, t.MarginalRate)).ToList(),
            year.TaxThresholds.Select(t => new TaxThresholdDto(t.Id, t.AgeGroup, t.ThresholdAmount)).ToList(),
            year.TaxRebates.Select(t => new TaxRebateDto(t.Id, t.RebateType, t.Amount)).ToList()));
    }
}
