using Payroll.Domain.Identity;

namespace Payroll.Application.Common.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(AppUser user, IList<string> roles);
    RefreshToken CreateRefreshToken();
}
