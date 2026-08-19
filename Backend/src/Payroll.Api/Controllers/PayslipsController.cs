using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Payslips.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
public class PayslipsController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>List payslips for an employee across payroll periods.</summary>
    [HttpGet("{employeeId:guid}")]
    public async Task<IActionResult> GetForEmployee(Guid employeeId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetPayslipsForEmployeeQuery(employeeId), ct));

    /// <summary>Download a single payslip as a PDF.</summary>
    [HttpGet("{payrollLineId:guid}/download")]
    public async Task<IActionResult> Download(Guid payrollLineId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GeneratePayslipPdfQuery(payrollLineId), ct);
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return File(result.Value!, "application/pdf", $"payslip-{payrollLineId}.pdf");
    }
}
