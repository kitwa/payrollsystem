using MediatR;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Tax.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Tax.Commands;

public record UpdateTaxTableCommand(Guid Id, UpdateTaxTableDto Dto) : IRequest<Result>;

public class UpdateTaxTableHandler(IAppDbContext db) : IRequestHandler<UpdateTaxTableCommand, Result>
{
    public async Task<Result> Handle(UpdateTaxTableCommand request, CancellationToken ct)
    {
        var table = await db.TaxTables.FindAsync([request.Id], ct);
        if (table is null) return Result.Fail("Tax table bracket not found.");

        var d = request.Dto;
        table.IncomeFrom = d.IncomeFrom;
        table.IncomeTo = d.IncomeTo;
        table.BaseTax = d.BaseTax;
        table.MarginalRate = d.MarginalRate;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record UpdateTaxThresholdCommand(Guid Id, UpdateTaxThresholdDto Dto) : IRequest<Result>;

public class UpdateTaxThresholdHandler(IAppDbContext db) : IRequestHandler<UpdateTaxThresholdCommand, Result>
{
    public async Task<Result> Handle(UpdateTaxThresholdCommand request, CancellationToken ct)
    {
        var threshold = await db.TaxThresholds.FindAsync([request.Id], ct);
        if (threshold is null) return Result.Fail("Tax threshold not found.");

        threshold.ThresholdAmount = request.Dto.ThresholdAmount;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record UpdateTaxRebateCommand(Guid Id, UpdateTaxRebateDto Dto) : IRequest<Result>;

public class UpdateTaxRebateHandler(IAppDbContext db) : IRequestHandler<UpdateTaxRebateCommand, Result>
{
    public async Task<Result> Handle(UpdateTaxRebateCommand request, CancellationToken ct)
    {
        var rebate = await db.TaxRebates.FindAsync([request.Id], ct);
        if (rebate is null) return Result.Fail("Tax rebate not found.");

        rebate.Amount = request.Dto.Amount;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
