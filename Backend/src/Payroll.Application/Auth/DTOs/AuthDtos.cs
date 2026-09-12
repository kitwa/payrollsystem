namespace Payroll.Application.Auth.DTOs;

public record LoginDto(string Email, string Password);

public record RegisterCompanyDto(
    string CompanyName,
    string RegistrationNumber,
    string? TaxNumber,
    string? Phone,
    string? CompanyEmail,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string Password);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime Expires,
    string Email,
    string FirstName,
    string LastName,
    IList<string> Roles,
    Guid UserId,
    Guid? CompanyId,
    Guid? EmployeeId);

public record RefreshTokenDto(string RefreshToken);

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(string Email, string Token, string NewPassword);
public record ChangePasswordDto(string CurrentPassword, string NewPassword);
