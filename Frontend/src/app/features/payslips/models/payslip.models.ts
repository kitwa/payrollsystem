export interface Payslip {
  payrollLineId: string;
  payrollPeriodId: string;
  year: number;
  month: number;
  status: number;
  grossEarnings: number;
  totalDeductions: number;
  netPay: number;
}
