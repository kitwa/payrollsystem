export enum PayrollStatus {
  Draft = 0,
  Approved = 1,
  Locked = 2,
  Paid = 3
}

export interface PayrollPeriod {
  id: string;
  companyId: string;
  year: number;
  month: number;
  periodStart: string;
  periodEnd: string;
  status: PayrollStatus;
  employeeCount: number;
  totalGross: number;
  totalDeductions: number;
  totalNet: number;
}

export interface Earning {
  category: number;
  description: string;
  amount: number;
}

export interface Deduction {
  category: number;
  description: string;
  employeeAmount: number;
  employerAmount: number;
}

export interface PayrollLine {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  grossEarnings: number;
  totalDeductions: number;
  netPay: number;
  earnings: Earning[];
  deductions: Deduction[];
}

export interface GeneratePayroll {
  companyId: string;
  year: number;
  month: number;
  employeeIds: string[];
}
