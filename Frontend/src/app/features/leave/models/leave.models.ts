export enum LeaveStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Cancelled = 3
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  days: number;
  reason?: string;
  status: LeaveStatus;
  reviewedBy?: string;
  reviewedAt?: string;
}

export interface CreateLeaveRequest {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  reason?: string;
}

export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeName: string;
  entitlementDays: number;
  usedDays: number;
  balanceDays: number;
}

export interface LeaveType {
  id: string;
  companyId: string;
  name: string;
  defaultEntitlementDays: number;
  isPaid: boolean;
  requiresApproval: boolean;
  isActive: boolean;
}
