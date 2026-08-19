using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Auth.Commands;
using Payroll.Application.Auth.DTOs;

namespace Payroll.Api.Controllers;

public class AuthController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Register a new company and its admin user.</summary>
    [AllowAnonymous]
    [HttpPost("register-company")]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new RegisterCompanyCommand(dto), ct));

    /// <summary>Authenticate and receive JWT + refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new LoginCommand(dto), ct));

    /// <summary>Exchange a valid refresh token for a new token pair.</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new RefreshTokenCommand(dto.RefreshToken), ct));
}
