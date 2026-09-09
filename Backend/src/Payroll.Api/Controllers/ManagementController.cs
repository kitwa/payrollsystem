using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Management.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Roles = Constants.Roles.SuperAdmin)]
public class ManagementController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetCompanyManagementSummaryQuery(companyId), ct));

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] Guid? companyId, [FromQuery] PaginationParams p, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetManagementAuditQuery(companyId, p), ct));
}
