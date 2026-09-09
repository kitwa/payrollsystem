using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Payslips.Commands;
using Payroll.Application.Payslips.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
public class PayslipsController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Payslip batches per payroll period for the company.</summary>
    [HttpGet("periods")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> GetPeriods([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayslipPeriodsQuery(companyId), ct));

    /// <summary>All payslips within a payroll period.</summary>
    [HttpGet("period/{periodId:guid}")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> GetForPeriod(Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayslipsForPeriodQuery(periodId), ct));

    /// <summary>Payslips belonging to the signed-in employee.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetMyPayslipsQuery(), ct));

    /// <summary>Payslips for a specific employee.</summary>
    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> GetForEmployee(Guid employeeId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayslipsForEmployeeQuery(employeeId), ct));

    /// <summary>Full payslip breakdown used for on-screen preview.</summary>
    [HttpGet("{payrollLineId:guid}/detail")]
    public async Task<IActionResult> GetDetail(Guid payrollLineId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayslipDetailQuery(payrollLineId), ct));

    /// <summary>Download a single payslip as a PDF.</summary>
    [HttpGet("{payrollLineId:guid}/download")]
    public async Task<IActionResult> Download(Guid payrollLineId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GeneratePayslipPdfQuery(payrollLineId), ct);
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return File(result.Value!, "application/pdf", $"payslip-{payrollLineId}.pdf");
    }

    /// <summary>Download every payslip in a period as a single ZIP archive.</summary>
    [HttpGet("period/{periodId:guid}/download-all")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> DownloadAll(Guid periodId, CancellationToken ct)
    {
        var result = await Mediator.Send(new DownloadPeriodPayslipsQuery(periodId), ct);
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return File(result.Value!, "application/zip", $"payslips-{periodId}.zip");
    }

    /// <summary>Email a single payslip to the employee.</summary>
    [HttpPost("{payrollLineId:guid}/email")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> Email(Guid payrollLineId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new EmailPayslipCommand(payrollLineId), ct));

    /// <summary>Email every payslip in a payroll period.</summary>
    [HttpPost("period/{periodId:guid}/email-all")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> EmailAll(Guid periodId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new EmailPeriodPayslipsCommand(periodId), ct));
}
