using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Companies.Commands;
using Payroll.Application.Companies.DTOs;
using Payroll.Application.Companies.Queries;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[Authorize(Policy = Constants.Policies.RequireAdminRole)]
public class CompaniesController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Get a company profile by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetCompanyByIdQuery(id), ct));

    /// <summary>Update a company profile.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateCompanyCommand(id, dto), ct));
}
