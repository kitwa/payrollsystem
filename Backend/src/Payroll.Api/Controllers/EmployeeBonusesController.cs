using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Payroll;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
[Route("api/employee-bonuses")]
public class EmployeeBonusesController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, [FromQuery] Guid payrollPeriodId,
        [FromQuery] Guid? employeeId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetEmployeeBonusesQuery(companyId, payrollPeriodId, employeeId), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeBonusDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateEmployeeBonusCommand(dto), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeBonusDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateEmployeeBonusCommand(id, dto), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new DeleteEmployeeBonusCommand(id), ct));
}
