using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Tax.Commands;
using Payroll.Application.Tax.DTOs;
using Payroll.Application.Tax.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireAdminRole)]
public class TaxController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>List all tax years.</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetTaxYearsQuery(), ct));

    /// <summary>Get tax year detail (tables, thresholds, rebates). Defaults to the active tax year.</summary>
    [HttpGet("year")]
    public async Task<IActionResult> GetYearDetail([FromQuery] Guid? taxYearId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetTaxYearDetailQuery(taxYearId), ct));

    /// <summary>Update a PAYE tax bracket.</summary>
    [HttpPut("tables/{id:guid}")]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateTaxTableDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateTaxTableCommand(id, dto), ct));

    /// <summary>Update an age-based tax threshold.</summary>
    [HttpPut("thresholds/{id:guid}")]
    public async Task<IActionResult> UpdateThreshold(Guid id, [FromBody] UpdateTaxThresholdDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateTaxThresholdCommand(id, dto), ct));

    /// <summary>Update a tax rebate amount.</summary>
    [HttpPut("rebates/{id:guid}")]
    public async Task<IActionResult> UpdateRebate(Guid id, [FromBody] UpdateTaxRebateDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateTaxRebateCommand(id, dto), ct));
}
