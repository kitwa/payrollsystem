using MediatR;
using Microsoft.AspNetCore.Identity;
using Payroll.Application.Auth.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;
using Payroll.Shared;

namespace Payroll.Application.Auth.Commands;

public record LoginCommand(LoginDto Dto) : IRequest<Result<AuthResponseDto>>;

public class LoginHandler(
    UserManager<AppUser> userManager,
    ITokenService tokenService) : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Dto.Email);
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.Fail("Invalid credentials.");

        if (!await userManager.CheckPasswordAsync(user, request.Dto.Password))
            return Result<AuthResponseDto>.Fail("Invalid credentials.");

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();

        user.RefreshTokens.RemoveAll(t => !t.IsActive);
        user.RefreshTokens.Add(refreshToken);
        await userManager.UpdateAsync(user);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto(
            accessToken,
            refreshToken.Token,
            refreshToken.Expires,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            user.Id,
            user.CompanyId,
            user.EmployeeId));
    }
}
