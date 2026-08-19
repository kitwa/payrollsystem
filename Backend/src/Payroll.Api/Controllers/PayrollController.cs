using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Payroll.Commands;
using Payroll.Application.Payroll.DTOs;
using Payroll.Application.Payroll.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
public class PayrollController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>List payroll periods for a company.</summary>
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayrollPeriodsQuery(companyId), ct));

    /// <summary>Get a single payroll period by ID.</summary>
    [HttpGet("{periodId:guid}")]
    public async Task<IActionResult> GetById(Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayrollPeriodByIdQuery(periodId), ct));

    /// <summary>Get all employee lines for a payroll period.</summary>
    [HttpGet("{periodId:guid}/lines")]
    public async Task<IActionResult> GetLines(Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayrollPeriodLinesQuery(periodId), ct));

    /// <summary>Generate a new payroll period (creates draft with all employee lines).</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GeneratePayrollDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GeneratePayrollCommand(dto), ct));

    /// <summary>Approve a draft payroll period.</summary>
    [HttpPost("{periodId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid periodId, CancellationToken ct)
    {
        var user = User.Identity?.Name ?? "system";
        return FromResult(await Mediator.Send(new ApprovePayrollCommand(periodId, user), ct));
    }

    /// <summary>Lock an approved payroll period — no further changes allowed.</summary>
    [HttpPost("{periodId:guid}/lock")]
    public async Task<IActionResult> Lock(Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new LockPayrollCommand(periodId), ct));

    /// <summary>Mark a locked payroll period as paid.</summary>
    [HttpPost("{periodId:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new MarkPaidCommand(periodId), ct));
}
