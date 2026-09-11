export enum DeductionCategory {
  Paye = 0,
  Uif = 1,
  Sdl = 2,
  MedicalAid = 3,
  Pension = 4,
  ProvidentFund = 5,
  RetirementAnnuity = 6,
  Loan = 7,
  Advance = 8,
  Other = 9
}

export interface EmployeeDeduction {
  id: string;
  employeeId: string;
  deductionTypeId?: string;
  description: string;
  category: DeductionCategory;
  employeeAmount: number;
  employerAmount: number;
  isActive: boolean;
  payrollPeriodId?: string | null;
}
