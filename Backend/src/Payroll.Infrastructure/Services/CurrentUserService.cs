using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Payroll.Application.Common.Interfaces;
using Payroll.Shared;

namespace Payroll.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    public string Email => User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("email")
        ?? string.Empty;

    public Guid? CompanyId => Guid.TryParse(User.FindFirstValue(Constants.ClaimTypes.CompanyId), out var id)
        ? id
        : null;

    public Guid? EmployeeId => Guid.TryParse(User.FindFirstValue(Constants.ClaimTypes.EmployeeId), out var id)
        ? id
        : null;

    public bool IsInRole(string role) => User.IsInRole(role);
}
