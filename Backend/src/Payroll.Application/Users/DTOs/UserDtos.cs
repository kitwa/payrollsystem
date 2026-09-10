namespace Payroll.Application.Users.DTOs;

public record UserListDto(
    Guid UserId,
    Guid? EmployeeId,
    string? EmployeeName,
    string Email,
    string FirstName,
    string LastName,
    Guid? CompanyId,
    IList<string> Roles,
    bool IsActive);

public record CreateUserDto(
    Guid CompanyId,
    Guid EmployeeId,
    string Password,
    string Role);

public record CreateSuperAdminUserDto(
    string Email,
    string FirstName,
    string LastName,
    string Password);

public record UpdateUserRolesDto(IList<string> Roles);

public record UpdateUserStatusDto(bool IsActive);
