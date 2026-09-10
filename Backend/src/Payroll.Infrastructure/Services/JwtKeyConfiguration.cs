using Microsoft.Extensions.Configuration;

namespace Payroll.Infrastructure.Services;

public static class JwtKeyConfiguration
{
    public static byte[] GetKeyBytes(IConfiguration configuration)
    {
        var encodedKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(encodedKey))
            throw new InvalidOperationException("JWT signing key is missing. Configure Jwt:Key with a base64-encoded secret containing at least 64 bytes.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(encodedKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("JWT signing key is invalid. Jwt:Key must be a base64-encoded secret.", ex);
        }

        if (key.Length < 64)
            throw new InvalidOperationException($"JWT signing key is too short. Jwt:Key must decode to at least 64 bytes for HS512; it decoded to {key.Length} bytes.");

        return key;
    }
}