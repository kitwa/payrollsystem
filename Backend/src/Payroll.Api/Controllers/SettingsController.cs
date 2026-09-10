using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Settings.Commands;
using Payroll.Application.Settings.DTOs;
using Payroll.Application.Settings.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
public class SettingsController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>List leave types available to a company (includes global templates).</summary>
    [HttpGet("leave-types")]
    [Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
    public async Task<IActionResult> GetLeaveTypes([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetLeaveTypesQuery(companyId), ct));

    /// <summary>Create a new leave type for a company.</summary>
    [HttpPost("leave-types")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateLeaveTypeCommand(dto), ct));

    /// <summary>Update a leave type.</summary>
    [HttpPut("leave-types/{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> UpdateLeaveType(Guid id, [FromBody] UpdateLeaveTypeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateLeaveTypeCommand(id, dto), ct));

    /// <summary>List earning types configured for a company.</summary>
    [HttpGet("earning-types")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> GetEarningTypes([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetEarningTypesQuery(companyId), ct));

    /// <summary>Create a new earning type for a company.</summary>
    [HttpPost("earning-types")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> CreateEarningType([FromBody] CreateEarningTypeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateEarningTypeCommand(dto), ct));

    /// <summary>Update an earning type.</summary>
    [HttpPut("earning-types/{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> UpdateEarningType(Guid id, [FromBody] UpdateEarningTypeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateEarningTypeCommand(id, dto), ct));

    /// <summary>List deduction types configured for a company.</summary>
    [HttpGet("deduction-types")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> GetDeductionTypes([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetDeductionTypesQuery(companyId), ct));

    /// <summary>Create a new deduction type for a company.</summary>
    [HttpPost("deduction-types")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> CreateDeductionType([FromBody] CreateDeductionTypeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateDeductionTypeCommand(dto), ct));

    /// <summary>Update a deduction type.</summary>
    [HttpPut("deduction-types/{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> UpdateDeductionType(Guid id, [FromBody] UpdateDeductionTypeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateDeductionTypeCommand(id, dto), ct));
}
