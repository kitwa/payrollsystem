using Payroll.Domain.Leave.Enums;

namespace Payroll.Application.Leave.DTOs;

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    DateTime StartDate,
    DateTime EndDate,
    decimal Days,
    string? Reason,
    LeaveStatus Status,
    string? ReviewedBy,
    DateTime? ReviewedAt);

public record CreateLeaveRequestDto(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason);

public record LeaveBalanceDto(
    Guid LeaveTypeId,
    string LeaveTypeName,
    decimal EntitlementDays,
    decimal UsedDays,
    decimal BalanceDays);

public record EmployeeLeaveBalanceDto(
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    List<LeaveBalanceDto> Balances);
