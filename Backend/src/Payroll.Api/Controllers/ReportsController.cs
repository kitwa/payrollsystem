using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Reports.Queries;
using Payroll.Domain.PayrollRuns.Enums;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
public class ReportsController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Payroll register — gross, deductions, and net per employee for a period.</summary>
    [HttpGet("payroll-register")]
    public async Task<IActionResult> PayrollRegister([FromQuery] Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayrollRegisterQuery(periodId), ct));

    /// <summary>Leave report — leave requests within a date range for a company.</summary>
    [HttpGet("leave-report")]
    public async Task<IActionResult> LeaveReport([FromQuery] Guid companyId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetLeaveReportQuery(companyId, from, to), ct));

    /// <summary>UIF report for a payroll period.</summary>
    [HttpGet("uif-report")]
    public async Task<IActionResult> UifReport([FromQuery] Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetStatutoryReportQuery(periodId, DeductionCategory.Uif), ct));

    /// <summary>SDL report for a payroll period.</summary>
    [HttpGet("sdl-report")]
    public async Task<IActionResult> SdlReport([FromQuery] Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetStatutoryReportQuery(periodId, DeductionCategory.Sdl), ct));

    /// <summary>PAYE tax report for a payroll period.</summary>
    [HttpGet("tax-report")]
    public async Task<IActionResult> TaxReport([FromQuery] Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetStatutoryReportQuery(periodId, DeductionCategory.Paye), ct));

    /// <summary>Employee cost report — total employer cost per employee for a tax year.</summary>
    [HttpGet("employee-cost")]
    public async Task<IActionResult> EmployeeCost([FromQuery] Guid companyId, [FromQuery] int year, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetEmployeeCostReportQuery(companyId, year), ct));
}
