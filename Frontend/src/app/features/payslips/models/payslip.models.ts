export interface Payslip {
  payrollLineId: string;
  payrollPeriodId: string;
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  email?: string;
  year: number;
  month: number;
  status: number;
  grossEarnings: number;
  totalDeductions: number;
  netPay: number;
  emailedAt?: string | null;
}

export interface PayslipPeriodSummary {
  payrollPeriodId: string;
  year: number;
  month: number;
  periodStart: string;
  periodEnd: string;
  status: number;
  payslipCount: number;
  emailedCount: number;
  totalNet: number;
}

export interface PayslipLineItem {
  category: string;
  description: string;
  amount: number;
}

export interface PayslipDetail {
  payrollLineId: string;
  payrollPeriodId: string;
  companyName: string;
  employeeName: string;
  employeeNumber: string;
  email?: string;
  jobTitle?: string;
  department?: string;
  taxNumber?: string;
  uifNumber?: string;
  year: number;
  month: number;
  periodStart: string;
  periodEnd: string;
  status: number;
  grossEarnings: number;
  totalDeductions: number;
  netPay: number;
  emailedAt?: string | null;
  earnings: PayslipLineItem[];
  deductions: PayslipLineItem[];
}

export interface EmailPayslipResult {
  sent: number;
  skipped: number;
  failures: string[];
}
