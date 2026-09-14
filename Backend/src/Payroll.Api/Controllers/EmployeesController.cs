using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Employees.Commands;
using Payroll.Application.Employees.DTOs;
using Payroll.Application.Employees.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
public class EmployeesController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Paginated list of employees for a company.</summary>
    [HttpGet]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, [FromQuery] PaginationParams p, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetEmployeesQuery(companyId, p), ct));

    /// <summary>Get a single employee by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetEmployeeByIdQuery(id), ct));

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetMyEmployeeQuery(), ct));

    /// <summary>Create a new employee.</summary>
    [HttpPost]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateEmployeeCommand(dto), ct));

    /// <summary>Update employee details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateEmployeeCommand(id, dto), ct));

    [HttpPut("{id:guid}/bank-details")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> UpdateBankDetails(Guid id, [FromBody] BankDetailsDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateBankDetailsCommand(id, dto), ct));

    /// <summary>Terminate an employee.</summary>
    [HttpPost("{id:guid}/terminate")]
    [Authorize(Policy = Constants.Policies.RequirePayrollManagerRole)]
    public async Task<IActionResult> Terminate(Guid id, [FromQuery] DateTime terminationDate, CancellationToken ct) =>
        FromResult(await Mediator.Send(new TerminateEmployeeCommand(id, terminationDate), ct));

    /// <summary>Delete an employee. Admin and Super Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Constants.Policies.RequireAdminRole)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new DeleteEmployeeCommand(id), ct));
}
