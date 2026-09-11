using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Tax.Commands;
using Payroll.Application.Tax.DTOs;
using Payroll.Application.Tax.Queries;
using Payroll.Application.Tax;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Tax;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
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
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateTaxTableDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateTaxTableCommand(id, dto), ct));

    /// <summary>Update an age-based tax threshold.</summary>
    [HttpPut("thresholds/{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> UpdateThreshold(Guid id, [FromBody] UpdateTaxThresholdDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateTaxThresholdCommand(id, dto), ct));

    /// <summary>Update a tax rebate amount.</summary>
    [HttpPut("rebates/{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> UpdateRebate(Guid id, [FromBody] UpdateTaxRebateDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateTaxRebateCommand(id, dto), ct));
}

[Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
[Route("api/tax-certificates")]
public class TaxCertificatesController(IMediator mediator, ITaxCertificatePdfService pdfService) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid companyId, [FromQuery] Guid taxYearId, [FromQuery] string? search, [FromQuery] TaxCertificateStatus? status, [FromQuery] TaxCertificateType? type, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetTaxCertificatesQuery(companyId, taxYearId, search, status, type), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) => FromResult(await Mediator.Send(new GetTaxCertificateQuery(id), ct));

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateTaxCertificateCommand command, CancellationToken ct) => FromResult(await Mediator.Send(command, ct));

    [HttpPost("generate-all")]
    public async Task<IActionResult> GenerateAll([FromBody] GenerateAllTaxCertificatesCommand command, CancellationToken ct) => FromResult(await Mediator.Send(command, ct));

    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct) => FromResult(await Mediator.Send(new FinalizeTaxCertificateCommand(id), ct));

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var detail = await Mediator.Send(new GetTaxCertificateQuery(id), ct);
        if (!detail.IsSuccess) return BadRequest(new { errors = detail.Errors });
        var bytes = await pdfService.GenerateAsync(id, ct);
        return File(bytes, "application/pdf", $"tax-certificate-{id}.pdf");
    }
}
