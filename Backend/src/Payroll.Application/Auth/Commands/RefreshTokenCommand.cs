using MediatR;
using Microsoft.AspNetCore.Identity;
using Payroll.Application.Auth.DTOs;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;
using Payroll.Shared;

namespace Payroll.Application.Auth.Commands;

public record RefreshTokenCommand(string Token) : IRequest<Result<AuthResponseDto>>;

public class RefreshTokenHandler(
    UserManager<AppUser> userManager,
    ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var users = userManager.Users.ToList();
        var user = users.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Token == request.Token));

        if (user is null) return Result<AuthResponseDto>.Fail("Invalid refresh token.");

        var oldToken = user.RefreshTokens.Single(t => t.Token == request.Token);
        if (!oldToken.IsActive) return Result<AuthResponseDto>.Fail("Refresh token is expired or revoked.");

        oldToken.Revoked = DateTime.UtcNow;
        var newRefreshToken = tokenService.CreateRefreshToken();
        user.RefreshTokens.Add(newRefreshToken);
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto(
            accessToken,
            newRefreshToken.Token,
            newRefreshToken.Expires,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            user.Id,
            user.CompanyId,
            user.EmployeeId));
    }
}
