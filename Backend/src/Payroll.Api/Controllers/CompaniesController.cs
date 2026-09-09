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
    /// <summary>List active companies for SuperAdmin tenant selection.</summary>
    [HttpGet]
    [Authorize(Roles = Constants.Roles.SuperAdmin)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetCompaniesQuery(), ct));

    /// <summary>Get a company profile by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GetCompanyByIdQuery(id), ct));

    /// <summary>Update a company profile.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateCompanyCommand(id, dto), ct));

    /// <summary>Upload or replace the company logo shown on payslips.</summary>
    [HttpPost("{id:guid}/logo")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        return FromResult(await Mediator.Send(new UploadCompanyLogoCommand(id, stream.ToArray(), file.ContentType), ct));
    }

    /// <summary>Serve the company logo image. Public so it can be used directly in &lt;img&gt; tags.</summary>
    [HttpGet("{id:guid}/logo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLogo(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCompanyLogoQuery(id), ct);
        if (!result.IsSuccess) return NotFound();
        return File(result.Value!.Data, result.Value!.ContentType);
    }

    /// <summary>Remove the company logo.</summary>
    [HttpDelete("{id:guid}/logo")]
    public async Task<IActionResult> RemoveLogo(Guid id, CancellationToken ct) =>
        FromResult(await Mediator.Send(new RemoveCompanyLogoCommand(id), ct));
}
