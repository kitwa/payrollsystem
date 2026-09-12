using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Payroll.Application.Auth.Commands;
using Payroll.Application.Auth.DTOs;

namespace Payroll.Api.Controllers;

public class AuthController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>Register a new company and its admin user.</summary>
    [AllowAnonymous]
    [HttpPost("register-company")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new RegisterCompanyCommand(dto), ct));

    /// <summary>Authenticate and receive JWT + refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new LoginCommand(dto), ct));

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new ForgotPasswordCommand(dto), ct));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new ResetPasswordCommand(dto), ct));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new ChangePasswordCommand(dto), ct));

    /// <summary>Exchange a valid refresh token for a new token pair.</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken ct) =>
        FromResult(await Mediator.Send(new RefreshTokenCommand(dto.RefreshToken), ct));
}
