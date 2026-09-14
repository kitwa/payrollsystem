using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Admin;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

/// <summary>Super Admin company management — disable/enable, and cross-company subscription visibility.</summary>
[Authorize(Roles = Constants.Roles.SuperAdmin)]
[Route("api/admin/companies")]
public class AdminCompaniesController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetAdminCompaniesQuery(), ct));

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new DisableCompanyCommand(id), ct));

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new EnableCompanyCommand(id), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new DeleteCompanyCommand(id), ct));

    [HttpPut("{id:guid}/activity-history")]
    public async Task<IActionResult> SetActivityHistory(Guid id, [FromBody] SetActivityHistoryRequest request, CancellationToken ct) =>
        FromResult(await Mediator.Send(new SetActivityHistoryCommand(id, request.Enabled), ct));

    /// <summary>Enable or disable Access &amp; Activity History for every company at once.</summary>
    [HttpPut("activity-history/all")]
    public async Task<IActionResult> SetActivityHistoryForAll([FromBody] SetActivityHistoryRequest request, CancellationToken ct) =>
        FromResult(await Mediator.Send(new SetActivityHistoryForAllCompaniesCommand(request.Enabled), ct));
}

public record SetActivityHistoryRequest(bool Enabled);
