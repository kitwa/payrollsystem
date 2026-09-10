export interface CompanyManagementSummary {
  companyId: string;
  companyName: string;
  userCount: number;
  activeUserCount: number;
  employeeCount: number;
  activeEmployeeCount: number;
  payrollPeriodCount: number;
  payslipCount: number;
  emailedPayslipCount: number;
  latestPayrollNet: number;
  latestPayrollDate?: string | null;
  lastActivityAt?: string | null;
}

export interface ManagementAudit {
  id: string;
  companyId?: string | null;
  userEmail?: string | null;
  ipAddress?: string | null;
  httpMethod: string;
  path: string;
  statusCode: number;
  action?: string | null;
  occurredAt: string;
}

export interface ManagementAuditPage {
  items: ManagementAudit[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminCompanyDto {
  id: string;
  name: string;
  isActive: boolean;
  subscriptionStatus: string;
  planCode: string;
  planName: string;
  employeeCount: number;
  maxEmployees?: number | null;
  trialEndDate?: string | null;
  currentPeriodEnd?: string | null;
  createdAt: string;
}

