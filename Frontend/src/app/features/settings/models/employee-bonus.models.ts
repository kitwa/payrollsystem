export interface EmployeeBonus {
  id: string;
  employeeId: string;
  payrollPeriodId: string;
  earningTypeId?: string;
  description: string;
  amount: number;
  notes?: string;
}

export interface CreateEmployeeBonus {
  companyId: string;
  employeeId: string;
  payrollPeriodId: string;
  earningTypeId: string;
  description: string;
  amount: number;
  notes?: string;
}
