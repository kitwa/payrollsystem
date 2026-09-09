using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Employees;
using Payroll.Application.Employees.DTOs;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
public class DepartmentsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetDepartmentsQuery(companyId), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateDepartmentCommand(dto), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateDepartmentCommand(id, dto), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new DeleteDepartmentCommand(id), ct));
}
