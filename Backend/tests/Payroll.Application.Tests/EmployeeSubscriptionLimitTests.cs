using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Payroll.Application.Billing;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Employees.Commands;
using Payroll.Application.Employees.DTOs;
using Payroll.Domain.Billing;
using Payroll.Domain.Companies;
using Payroll.Domain.Employees;
using Payroll.Domain.Employees.Enums;
using Payroll.Domain.Identity;
using Payroll.Infrastructure.Persistence;
using SharedConstants = Payroll.Shared.Constants;

namespace Payroll.Application.Tests;

/// <summary>Confirms the plan employee-limit is actually enforced when creating employees, end to end.</summary>
public class EmployeeSubscriptionLimitTests
{
    private readonly AppDbContext _db;

    public EmployeeSubscriptionLimitTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    private async Task<Guid> SeedCompanyOnPlanAsync(string planCode, int? maxEmployees)
    {
        var company = new Company { Name = "Acme", IsActive = true };
        _db.Companies.Add(company);
        _db.CompanySubscriptions.Add(new CompanySubscription
        {
            CompanyId = company.Id,
            PlanCode = planCode,
            Status = SubscriptionStatus.Active,
            Price = 0
        });
        await _db.SaveChangesAsync();

        for (var i = 0; i < maxEmployees; i++)
        {
            _db.Employees.Add(new Employee
            {
                CompanyId = company.Id,
                EmployeeNumber = $"EMP{i:D4}",
                FirstName = "Test",
                LastName = $"Employee{i}",
                IdNumber = $"ID{i:D10}",
                DateOfBirth = new DateTime(1990, 1, 1),
                StartDate = DateTime.UtcNow,
                BasicSalary = 10000
            });
        }
        await _db.SaveChangesAsync();

        return company.Id;
    }

    private static CreateEmployeeHandler CreateHandler(AppDbContext db, ICurrentUser currentUser) =>
        new(db, currentUser, new SubscriptionService(db),
            Substitute.For<UserManager<AppUser>>(Substitute.For<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null),
            Substitute.For<IEmailService>(),
            Substitute.For<ISupportNotificationSettings>(),
            Substitute.For<ILogger<CreateEmployeeHandler>>());

    [Fact]
    public async Task CreateEmployee_is_blocked_once_the_plan_employee_limit_is_reached()
    {
        var companyId = await SeedCompanyOnPlanAsync("SMALL_BUSINESS", maxEmployees: 5);

        var currentUser = new FakeCurrentUser(companyId, isAdmin: true);
        var handler = CreateHandler(_db, currentUser);

        var dto = new CreateEmployeeDto(
            companyId, "New", "Hire", "ID9999999999", new DateTime(1990, 1, 1),
            Gender.Male, null, null, null, EmploymentType.Permanent, PayFrequency.Monthly,
            null, null, DateTime.UtcNow, 10000);

        var result = await handler.Handle(new CreateEmployeeCommand(dto), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("maximum of 5 employees"));
    }

    [Fact]
    public async Task CreateEmployee_succeeds_when_under_the_plan_employee_limit()
    {
        var companyId = await SeedCompanyOnPlanAsync("SMALL_BUSINESS", maxEmployees: 4);

        var currentUser = new FakeCurrentUser(companyId, isAdmin: true);
        var handler = CreateHandler(_db, currentUser);

        var dto = new CreateEmployeeDto(
            companyId, "New", "Hire", "ID9999999999", new DateTime(1990, 1, 1),
            Gender.Male, null, null, null, EmploymentType.Permanent, PayFrequency.Monthly,
            null, null, DateTime.UtcNow, 10000);

        var result = await handler.Handle(new CreateEmployeeCommand(dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private sealed class FakeCurrentUser(Guid companyId, bool isAdmin) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public string Email => "admin@acme.test";
        public Guid? CompanyId => companyId;
        public Guid? EmployeeId => null;
        public bool IsInRole(string role) => isAdmin && role == SharedConstants.Roles.Admin;
    }
}
