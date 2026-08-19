namespace Payroll.Application.Settings.DTOs;

public record LeaveTypeDto(Guid Id, Guid CompanyId, string Name, decimal DefaultEntitlementDays, bool IsPaid, bool RequiresApproval, bool IsActive);
public record CreateLeaveTypeDto(Guid CompanyId, string Name, decimal DefaultEntitlementDays, bool IsPaid, bool RequiresApproval);
public record UpdateLeaveTypeDto(string Name, decimal DefaultEntitlementDays, bool IsPaid, bool RequiresApproval, bool IsActive);

public record EarningTypeDto(Guid Id, Guid CompanyId, string Name, string Code, bool IsTaxable, bool IsActive);
public record CreateEarningTypeDto(Guid CompanyId, string Name, string Code, bool IsTaxable);
public record UpdateEarningTypeDto(string Name, string Code, bool IsTaxable, bool IsActive);

public record DeductionTypeDto(Guid Id, Guid CompanyId, string Name, string Code, bool IsEmployerContribution, bool IsActive);
public record CreateDeductionTypeDto(Guid CompanyId, string Name, string Code, bool IsEmployerContribution);
public record UpdateDeductionTypeDto(string Name, string Code, bool IsEmployerContribution, bool IsActive);
