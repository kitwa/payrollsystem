using Payroll.Application.Common.Interfaces;
using Payroll.Domain.PayrollRuns;
using Payroll.Shared;

namespace Payroll.Application.Payslips;

/// <summary>Central tenant/ownership rules for payslip access.</summary>
internal static class PayslipAccess
{
    internal static bool IsManager(ICurrentUser user) =>
        user.IsInRole(Constants.Roles.PayrollManager)
        || user.IsInRole(Constants.Roles.Admin)
        || user.IsInRole(Constants.Roles.SuperAdmin);

    internal static bool CanAccessEmployee(ICurrentUser user, Guid employeeId, Guid companyId)
    {
        if (user.IsInRole(Constants.Roles.SuperAdmin)) return true;

        if (IsManager(user))
            return user.CompanyId is not null && user.CompanyId == companyId;

        return user.EmployeeId is not null && user.EmployeeId == employeeId;
    }

    internal static bool CanAccessLine(ICurrentUser user, PayrollLine line) =>
        CanAccessEmployee(user, line.EmployeeId, line.Employee.CompanyId);

    internal static bool CanManageCompany(ICurrentUser user, Guid companyId)
    {
        if (user.IsInRole(Constants.Roles.SuperAdmin)) return true;
        return IsManager(user) && user.CompanyId is not null && user.CompanyId == companyId;
    }
}
