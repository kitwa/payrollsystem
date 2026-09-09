using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Employees.DTOs;
using Payroll.Domain.Employees;
using Payroll.Shared;

namespace Payroll.Application.Employees;

public record GetDepartmentsQuery(Guid CompanyId) : IRequest<Result<List<DepartmentDto>>>;

public class GetDepartmentsHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetDepartmentsQuery, Result<List<DepartmentDto>>>
{
    public async Task<Result<List<DepartmentDto>>> Handle(GetDepartmentsQuery request, CancellationToken ct)
    {
        var companyId = ResolveCompany(request.CompanyId, currentUser);
        if (companyId is null) return Result<List<DepartmentDto>>.Fail("You are not authorized to access this company's departments.");

        var departments = await db.Departments
            .Where(d => d.CompanyId == companyId && !d.IsDeleted)
            .OrderByDescending(d => d.IsSystemDepartment).ThenBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.CompanyId, d.Name, d.IsSystemDepartment))
            .ToListAsync(ct);

        return Result<List<DepartmentDto>>.Ok(departments);
    }

    private static Guid? ResolveCompany(Guid requestedCompanyId, ICurrentUser currentUser) =>
        currentUser.CompanyId ?? (currentUser.IsInRole(Constants.Roles.SuperAdmin) ? requestedCompanyId : null);
}

public record CreateDepartmentCommand(CreateDepartmentDto Dto) : IRequest<Result<Guid>>;

public class CreateDepartmentHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var companyId = ResolveCompany(request.Dto.CompanyId, currentUser);
        if (companyId is null) return Result<Guid>.Fail("You are not authorized to create a department for this company.");

        var name = request.Dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Result<Guid>.Fail("Department name is required.");

        var exists = await db.Departments.AnyAsync(d => d.CompanyId == companyId
            && d.Name.ToLower() == name.ToLower() && !d.IsDeleted, ct);
        if (exists) return Result<Guid>.Fail("A department with this name already exists.");

        var department = new Department { CompanyId = companyId.Value, Name = name };
        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(department.Id);
    }

    private static Guid? ResolveCompany(Guid requestedCompanyId, ICurrentUser currentUser) =>
        currentUser.CompanyId ?? (currentUser.IsInRole(Constants.Roles.SuperAdmin) ? requestedCompanyId : null);
}

public record UpdateDepartmentCommand(Guid Id, UpdateDepartmentDto Dto) : IRequest<Result>;

public class UpdateDepartmentHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<UpdateDepartmentCommand, Result>
{
    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, ct);
        if (department is null) return Result.Fail("Department not found.");
        if (currentUser.CompanyId is not null && department.CompanyId != currentUser.CompanyId)
            return Result.Fail("You are not authorized to update this department.");
        if (currentUser.CompanyId is null && !currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result.Fail("You are not authorized to update this department.");
        if (department.IsSystemDepartment)
            return Result.Fail("The General department is protected by default and cannot be renamed.");

        var name = request.Dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Result.Fail("Department name is required.");

        var exists = await db.Departments.AnyAsync(d => d.CompanyId == department.CompanyId && d.Id != department.Id
            && d.Name.ToLower() == name.ToLower() && !d.IsDeleted, ct);
        if (exists) return Result.Fail("A department with this name already exists.");

        var oldName = department.Name;
        department.Name = name;
        var assignedEmployees = await db.Employees
            .Where(e => e.CompanyId == department.CompanyId && e.Department == oldName && !e.IsDeleted)
            .ToListAsync(ct);
        foreach (var employee in assignedEmployees)
            employee.Department = name;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record DeleteDepartmentCommand(Guid Id) : IRequest<Result>;

public class DeleteDepartmentHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<DeleteDepartmentCommand, Result>
{
    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken ct)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, ct);
        if (department is null) return Result.Fail("Department not found.");
        if (currentUser.CompanyId is not null && department.CompanyId != currentUser.CompanyId)
            return Result.Fail("You are not authorized to delete this department.");
        if (currentUser.CompanyId is null && !currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result.Fail("You are not authorized to delete this department.");
        if (department.IsSystemDepartment)
            return Result.Fail("The General department is the company default and cannot be deleted.");

        var employeesAssigned = await db.Employees.AnyAsync(e => e.CompanyId == department.CompanyId
            && e.Department == department.Name && !e.IsDeleted, ct);
        if (employeesAssigned) return Result.Fail("Move assigned employees before deleting this department.");

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
