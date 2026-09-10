using Payroll.Domain.Employees.Enums;

namespace Payroll.Application.Employees.DTOs;

public record EmployeeListDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string? Email,
    string? JobTitle,
    string? Department,
    EmploymentStatus Status,
    DateTime StartDate);

public record EmployeeDto(
    Guid Id,
    Guid CompanyId,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string IdNumber,
    DateTime DateOfBirth,
    Gender Gender,
    string? Email,
    string? Phone,
    string? Address,
    string? TaxNumber,
    string? UifNumber,
    EmploymentStatus Status,
    EmploymentType EmploymentType,
    PayFrequency PayFrequency,
    string? JobTitle,
    string? Department,
    DateTime StartDate,
    DateTime? TerminationDate,
    decimal BasicSalary,
    BankDetailsDto? BankDetails);

public record BankDetailsDto(
    string BankName,
    string AccountNumber,
    string BranchCode,
    string AccountType);

public record CreateEmployeeDto(
    Guid CompanyId,
    string FirstName,
    string LastName,
    string IdNumber,
    DateTime DateOfBirth,
    Gender Gender,
    string? Email,
    string? Phone,
    string? TaxNumber,
    EmploymentType EmploymentType,
    PayFrequency PayFrequency,
    string? JobTitle,
    string? Department,
    DateTime StartDate,
    decimal BasicSalary);

public record UpdateEmployeeDto(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Address,
    string? TaxNumber,
    string? JobTitle,
    string? Department,
    decimal BasicSalary,
    EmploymentStatus Status);

public record DepartmentDto(Guid Id, Guid CompanyId, string Name, bool IsSystemDepartment, bool IsDefault);
public record CreateDepartmentDto(Guid CompanyId, string Name);
public record UpdateDepartmentDto(string Name);
