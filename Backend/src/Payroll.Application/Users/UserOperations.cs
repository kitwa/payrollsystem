using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Users.DTOs;
using Payroll.Domain.Audit;
using Payroll.Domain.Identity;
using Payroll.Shared;

namespace Payroll.Application.Users;

public record GetUsersQuery(Guid CompanyId) : IRequest<Result<List<UserListDto>>>;

public class GetUsersHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<GetUsersQuery, Result<List<UserListDto>>>
{
    public async Task<Result<List<UserListDto>>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        if (!TenantAccess.CanAccessCompany(currentUser, request.CompanyId))
            return Result<List<UserListDto>>.Fail("You are not authorized to view users for this company.");

        var users = await userManager.Users
            .Where(u => u.CompanyId == request.CompanyId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        var employeeIds = users.Where(u => u.EmployeeId.HasValue).Select(u => u.EmployeeId!.Value).ToList();
        var employeeNames = await db.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", ct);

        var result = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserListDto(
                user.Id,
                user.EmployeeId,
                user.EmployeeId.HasValue && employeeNames.TryGetValue(user.EmployeeId.Value, out var name) ? name : null,
                user.Email ?? user.UserName ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.CompanyId,
                roles,
                user.IsActive));
        }

        return Result<List<UserListDto>>.Ok(result);
    }
}

public record CreateUserCommand(CreateUserDto Dto) : IRequest<Result<Guid>>;

public class CreateUserHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        if (!TenantAccess.CanAccessCompany(currentUser, dto.CompanyId))
            return Result<Guid>.Fail("You are not authorized to create users for this company.");

        if (!CanAssignRole(dto.Role, currentUser))
            return Result<Guid>.Fail("You are not authorized to assign this role.");

        var employee = await db.Employees.FirstOrDefaultAsync(
            e => e.Id == dto.EmployeeId && e.CompanyId == dto.CompanyId && !e.IsDeleted, ct);
        if (employee is null) return Result<Guid>.Fail("Employee not found in this company.");

        if (await userManager.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId, ct))
            return Result<Guid>.Fail("This employee already has a user account.");

        var email = employee.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return Result<Guid>.Fail("The employee must have an email address before a user account can be created.");

        if (await userManager.FindByEmailAsync(email) is not null)
            return Result<Guid>.Fail("A user with this email address already exists.");

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            CompanyId = dto.CompanyId,
            EmployeeId = employee.Id,
            EmailConfirmed = false,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result<Guid>.Fail(createResult.Errors.Select(e => e.Description));

        var roleResult = await userManager.AddToRoleAsync(user, dto.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Result<Guid>.Fail(roleResult.Errors.Select(e => e.Description));
        }

        return Result<Guid>.Ok(user.Id);
    }

    private static bool CanAssignRole(string role, ICurrentUser user) =>
        user.IsInRole(Constants.Roles.SuperAdmin)
            ? role is Constants.Roles.Admin or Constants.Roles.PayrollManager or Constants.Roles.Employee
            : user.IsInRole(Constants.Roles.Admin)
                && role is Constants.Roles.PayrollManager or Constants.Roles.Employee;
}

public record CreateSuperAdminUserCommand(CreateSuperAdminUserDto Dto) : IRequest<Result<Guid>>;

public class CreateSuperAdminUserHandler(
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<CreateSuperAdminUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSuperAdminUserCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result<Guid>.Fail("Only Super Admin users can create Super Admin accounts.");

        var dto = request.Dto;
        var email = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return Result<Guid>.Fail("Email is required.");
        if (await userManager.FindByEmailAsync(email) is not null)
            return Result<Guid>.Fail("A user with this email address already exists.");

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            CompanyId = null,
            EmployeeId = null,
            EmailConfirmed = false,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result<Guid>.Fail(createResult.Errors.Select(e => e.Description));

        var roleResult = await userManager.AddToRoleAsync(user, Constants.Roles.SuperAdmin);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Result<Guid>.Fail(roleResult.Errors.Select(e => e.Description));
        }

        return Result<Guid>.Ok(user.Id);
    }
}

public record UpdateUserRolesCommand(Guid UserId, IList<string> Roles) : IRequest<Result>;

public class UpdateUserRolesHandler(
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<UpdateUserRolesCommand, Result>
{
    public async Task<Result> Handle(UpdateUserRolesCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return Result.Fail("User not found.");
        if (user.CompanyId is null || !TenantAccess.CanAccessCompany(currentUser, user.CompanyId.Value))
            return Result.Fail("You are not authorized to manage this user.");
        if (currentUser.UserId == user.Id)
            return Result.Fail("You cannot change your own role.");

        var requestedRoles = request.Roles
            .Select(role => role.Trim())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedRoles.Count == 0 || requestedRoles.Any(role => !CanAssignRole(role, currentUser)))
            return Result.Fail("You are not authorized to assign one or more requested roles.");

        var existingRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!removeResult.Succeeded) return Result.Fail(removeResult.Errors.Select(e => e.Description));

        var addResult = await userManager.AddToRolesAsync(user, requestedRoles);
        if (!addResult.Succeeded) return Result.Fail(addResult.Errors.Select(e => e.Description));

        return Result.Ok();
    }

    private static bool CanAssignRole(string role, ICurrentUser user) =>
        user.IsInRole(Constants.Roles.SuperAdmin)
            ? role is Constants.Roles.Admin or Constants.Roles.PayrollManager or Constants.Roles.Employee
            : user.IsInRole(Constants.Roles.Admin)
                && role is Constants.Roles.PayrollManager or Constants.Roles.Employee;
}

public record UpdateUserStatusCommand(Guid UserId, bool IsActive) : IRequest<Result>;

public class UpdateUserStatusHandler(
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<UpdateUserStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateUserStatusCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return Result.Fail("User not found.");
        if (user.CompanyId is null || !TenantAccess.CanAccessCompany(currentUser, user.CompanyId.Value))
            return Result.Fail("You are not authorized to manage this user.");
        if (currentUser.UserId == user.Id)
            return Result.Fail("You cannot deactivate your own account.");

        user.IsActive = request.IsActive;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => e.Description));
    }
}
