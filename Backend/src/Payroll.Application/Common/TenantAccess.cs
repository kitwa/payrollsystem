using Payroll.Application.Common.Interfaces;
using Payroll.Shared;

namespace Payroll.Application.Common;

public static class TenantAccess
{
    public static bool CanAccessCompany(ICurrentUser currentUser, Guid companyId) =>
        currentUser.IsInRole(Constants.Roles.SuperAdmin)
        || currentUser.CompanyId == companyId;

    public static bool CanAccessEmployee(ICurrentUser currentUser, Guid companyId, Guid employeeId) =>
        currentUser.IsInRole(Constants.Roles.SuperAdmin)
        || (currentUser.CompanyId == companyId
            && (currentUser.IsInRole(Constants.Roles.Admin)
                || currentUser.IsInRole(Constants.Roles.PayrollManager)))
        || (currentUser.CompanyId == companyId && currentUser.EmployeeId == employeeId);

    public static bool CanManageCompany(ICurrentUser currentUser, Guid companyId) =>
        currentUser.IsInRole(Constants.Roles.SuperAdmin)
        || (currentUser.CompanyId == companyId
            && (currentUser.IsInRole(Constants.Roles.Admin)
                || currentUser.IsInRole(Constants.Roles.PayrollManager)));
}
