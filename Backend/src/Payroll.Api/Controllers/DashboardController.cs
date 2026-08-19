using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Dashboard.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
public class DashboardController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Get KPI summary for the dashboard.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetDashboardSummaryQuery(companyId), ct));
}
