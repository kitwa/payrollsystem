using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Employees;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireAdminRole)]
[Route("api/employee-deductions")]
public class EmployeeDeductionsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, [FromQuery] Guid employeeId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetEmployeeDeductionsQuery(companyId, employeeId), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDeductionDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateEmployeeDeductionCommand(dto), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDeductionDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateEmployeeDeductionCommand(id, dto), ct));
}
