using FluentAssertions;
using Payroll.Domain.Employees;

namespace Payroll.Application.Tests;

public class DepartmentDefaultsTests
{
    [Fact]
    public void Default_department_uses_a_named_flag_for_the_company_default()
    {
        var department = new Department
        {
            Name = "General",
            IsDefault = true
        };

        department.IsSystemDepartment.Should().BeTrue();
        department.IsDefault.Should().BeTrue();
    }
}
