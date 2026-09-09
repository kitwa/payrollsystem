using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;

namespace Payroll.Infrastructure.Services;

public class TokenService(IConfiguration config) : ITokenService
{
    public string CreateAccessToken(AppUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        if (user.CompanyId.HasValue)
            claims.Add(new Claim(Shared.Constants.ClaimTypes.CompanyId, user.CompanyId.Value.ToString()));

        if (user.EmployeeId.HasValue)
            claims.Add(new Claim(Shared.Constants.ClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var secret = config["JwtSettings:Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(config["JwtSettings:ExpiryMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken CreateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            Expires = DateTime.UtcNow.AddDays(double.Parse(config["JwtSettings:RefreshTokenExpiryDays"] ?? "7"))
        };
    }
}
