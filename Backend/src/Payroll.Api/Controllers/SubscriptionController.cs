using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Billing.Commands;
using Payroll.Application.Billing.DTOs;
using Payroll.Application.Billing.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
[Route("api/subscription")]
public class SubscriptionController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Current company's subscription, plan, and employee usage.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetMySubscriptionQuery(), ct));

    /// <summary>All available pricing plans — the single source of truth for prices and employee limits.</summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPlansQuery(), ct));

    /// <summary>Upgrade or change the company's plan. Company administrators only.</summary>
    [HttpPost("upgrade")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpgradeSubscriptionCommand(dto), ct));

    /// <summary>Cancel the current subscription. Company administrators only.</summary>
    [HttpPost("cancel")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> Cancel(CancellationToken ct) =>
        FromResult(await Mediator.Send(new CancelSubscriptionCommand(), ct));
}
