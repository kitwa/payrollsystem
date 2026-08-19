namespace Payroll.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    Guid? CompanyId { get; }
    Guid? EmployeeId { get; }
    bool IsInRole(string role);
}
