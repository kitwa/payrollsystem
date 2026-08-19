using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected IActionResult FromResult(Result result) =>
        result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });

    protected IActionResult FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
}
