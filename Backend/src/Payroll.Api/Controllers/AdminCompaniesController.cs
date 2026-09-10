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
}
