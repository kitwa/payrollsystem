namespace Payroll.Shared;

/// <summary>Centralised string constants — never use magic strings.</summary>
public static class Constants
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string PayrollManager = "PayrollManager";
        public const string Employee = "Employee";
    }

    public static class Policies
    {
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequirePayrollManagerRole = "RequirePayrollManagerRole";
        public const string RequireEmployeeRole = "RequireEmployeeRole";
    }

    public static class ClaimTypes
    {
        public const string CompanyId = "companyId";
        public const string EmployeeId = "employeeId";
    }
}
