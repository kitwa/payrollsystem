using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Common;
using Payroll.Application.Employees.DTOs;
using Payroll.Domain.Employees;
using Payroll.Shared;

namespace Payroll.Application.Employees.Commands;

public record CreateEmployeeCommand(CreateEmployeeDto Dto) : IRequest<Result<Guid>>;

public class CreateEmployeeHandler(IAppDbContext db, ICurrentUser currentUser, ISubscriptionService subscriptionService) : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        if (!TenantAccess.CanManageCompany(currentUser, d.CompanyId))
            return Result<Guid>.Fail("You are not authorized to create an employee for this company.");

        var limitCheck = await subscriptionService.CheckCanAddEmployeeAsync(d.CompanyId, ct);
        if (!limitCheck.Allowed)
            return Result<Guid>.Fail(limitCheck.Message!);

        var idNumber = d.IdNumber.Trim();
        var duplicateIdNumber = await db.Employees.AnyAsync(
            e => e.CompanyId == d.CompanyId && e.IdNumber == idNumber && !e.IsDeleted, ct);
        if (duplicateIdNumber) return Result<Guid>.Fail("An employee with this ID number already exists.");

        var number = await GenerateEmployeeNumberAsync(d.CompanyId, ct);
        var department = string.IsNullOrWhiteSpace(d.Department)
            ? await db.Departments
                .Where(x => x.CompanyId == d.CompanyId && x.IsSystemDepartment && !x.IsDeleted)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct)
            : d.Department;

        var employee = new Employee
        {
            CompanyId = d.CompanyId,
            EmployeeNumber = number,
            FirstName = d.FirstName,
            LastName = d.LastName,
            IdNumber = idNumber,
            DateOfBirth = d.DateOfBirth,
            Gender = d.Gender,
            Email = d.Email,
            Phone = d.Phone,
            TaxNumber = d.TaxNumber,
            EmploymentType = d.EmploymentType,
            PayFrequency = d.PayFrequency,
            JobTitle = d.JobTitle,
            Department = department,
            StartDate = d.StartDate,
            BasicSalary = d.BasicSalary
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(employee.Id);
    }

    private async Task<string> GenerateEmployeeNumberAsync(Guid companyId, CancellationToken ct)
    {
        var count = await db.Employees.CountAsync(e => e.CompanyId == companyId, ct);
        return $"EMP{count + 1:D4}";
    }
}

public record UpdateEmployeeCommand(Guid Id, UpdateEmployeeDto Dto) : IRequest<Result>;

public class UpdateEmployeeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateEmployeeCommand, Result>
{
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([request.Id], ct);
        if (employee is null) return Result.Fail("Employee not found.");
        if (!TenantAccess.CanManageCompany(currentUser, employee.CompanyId))
            return Result.Fail("You are not authorized to update this employee.");

        var d = request.Dto;
        employee.FirstName = d.FirstName;
        employee.LastName = d.LastName;
        employee.Email = d.Email;
        employee.Phone = d.Phone;
        employee.Address = d.Address;
        employee.TaxNumber = d.TaxNumber;
        employee.JobTitle = d.JobTitle;
        employee.Department = d.Department;
        employee.BasicSalary = d.BasicSalary;
        employee.Status = d.Status;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record TerminateEmployeeCommand(Guid Id, DateTime TerminationDate) : IRequest<Result>;

public class TerminateEmployeeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<TerminateEmployeeCommand, Result>
{
    public async Task<Result> Handle(TerminateEmployeeCommand request, CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([request.Id], ct);
        if (employee is null) return Result.Fail("Employee not found.");
        if (!TenantAccess.CanManageCompany(currentUser, employee.CompanyId))
            return Result.Fail("You are not authorized to terminate this employee.");

        employee.Status = global::Payroll.Domain.Employees.Enums.EmploymentStatus.Terminated;
        employee.TerminationDate = request.TerminationDate;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
