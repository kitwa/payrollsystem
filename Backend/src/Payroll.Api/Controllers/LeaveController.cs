using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Leave.Commands;
using Payroll.Application.Leave.DTOs;
using Payroll.Application.Leave.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
public class LeaveController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>List leave requests for a company, optionally filtered by employee.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, [FromQuery] Guid? employeeId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetLeaveRequestsQuery(companyId, employeeId), ct));

    /// <summary>Get a single leave request by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetLeaveRequestByIdQuery(id), ct));

    /// <summary>Get leave balances for an employee for the current year.</summary>
    [HttpGet("balances/{employeeId:guid}")]
    public async Task<IActionResult> GetBalances(Guid employeeId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetLeaveBalancesQuery(employeeId), ct));

    /// <summary>Submit a leave request.</summary>
    [HttpPost("request")]
    public async Task<IActionResult> SubmitRequest([FromBody] CreateLeaveRequestDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new RequestLeaveCommand(dto), ct));

    /// <summary>Approve a pending leave request.</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var reviewer = User.Identity?.Name ?? "system";
        return FromResult(await Mediator.Send(new ApproveLeaveCommand(id, reviewer), ct));
    }

    /// <summary>Reject a pending leave request.</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] string note, CancellationToken ct)
    {
        var reviewer = User.Identity?.Name ?? "system";
        return FromResult(await Mediator.Send(new RejectLeaveCommand(id, reviewer, note), ct));
    }
}
