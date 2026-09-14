using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Users;
using Payroll.Application.Users.DTOs;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireAdminRole)]
public class UsersController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>List user accounts for a company.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetUsersQuery(companyId), ct));

    /// <summary>Create an employee login account and assign Employee or PayrollManager.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateUserCommand(dto), ct));

    [HttpPost("super-admin")]
    [Authorize(Roles = Constants.Roles.SuperAdmin)]
    public async Task<IActionResult> CreateSuperAdmin([FromBody] CreateSuperAdminUserDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateSuperAdminUserCommand(dto), ct));

    /// <summary>Replace a user's company roles. SuperAdmin cannot be granted through this endpoint.</summary>
    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateUserRolesCommand(id, dto.Roles), ct));

    /// <summary>Enable or disable a user account.</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateUserStatusCommand(id, dto.IsActive), ct));

    /// <summary>Delete a user account.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new DeleteUserCommand(id), ct));
}