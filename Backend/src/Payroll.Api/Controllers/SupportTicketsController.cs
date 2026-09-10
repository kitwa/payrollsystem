using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Support;
using Payroll.Domain.Support;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireAdminRole)]
[Route("api/support/tickets")]
public class SupportTicketsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] SupportTicketStatus? status,
        [FromQuery] SupportTicketType? type,
        [FromQuery] string? search,
        [FromQuery] PaginationParams p,
        CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetSupportTicketsQuery(companyId, status, type, search, p), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetSupportTicketQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateSupportTicketCommand(dto), ct));

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CloseSupportTicketCommand(id), ct));

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = Constants.Roles.SuperAdmin)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSupportTicketStatusDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateSupportTicketStatusCommand(id, dto.Status), ct));
}

public record UpdateSupportTicketStatusDto(SupportTicketStatus Status);