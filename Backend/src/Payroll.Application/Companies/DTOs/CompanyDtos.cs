namespace Payroll.Application.Companies.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string RegistrationNumber,
    string? TaxNumber,
    string? UifNumber,
    string? SdlNumber,
    string? PhysicalAddress,
    string? PostalAddress,
    string? Phone,
    string? Email,
    bool IsActive,
    bool HasLogo,
    bool IsUifEnabled,
    bool IsSdlEnabled);

public record UpdateCompanyDto(
    string Name,
    string RegistrationNumber,
    string? TaxNumber,
    string? UifNumber,
    string? SdlNumber,
    string? PhysicalAddress,
    string? PostalAddress,
    string? Phone,
    string? Email,
    bool IsUifEnabled,
    bool IsSdlEnabled);
