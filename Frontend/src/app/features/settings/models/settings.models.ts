export interface LeaveType {
  id: string;
  companyId: string;
  name: string;
  defaultEntitlementDays: number;
  isPaid: boolean;
  requiresApproval: boolean;
  isActive: boolean;
}

export interface CreateLeaveType {
  companyId: string;
  name: string;
  defaultEntitlementDays: number;
  isPaid: boolean;
  requiresApproval: boolean;
}

export interface UpdateLeaveType {
  name: string;
  defaultEntitlementDays: number;
  isPaid: boolean;
  requiresApproval: boolean;
  isActive: boolean;
}

export interface EarningType {
  id: string;
  companyId: string;
  name: string;
  code: string;
  isTaxable: boolean;
  isActive: boolean;
}

export interface DeductionType {
  id: string;
  companyId: string;
  name: string;
  code: string;
  isEmployerContribution: boolean;
  isActive: boolean;
}

export interface TaxTable {
  id: string;
  incomeFrom: number;
  incomeTo: number;
  baseTax: number;
  marginalRate: number;
}

export interface TaxThreshold {
  id: string;
  ageGroup: string;
  thresholdAmount: number;
}

export interface TaxRebate {
  id: string;
  rebateType: string;
  amount: number;
}

export interface TaxYearDetail {
  id: string;
  year: number;
  startDate: string;
  endDate: string;
  uifMonthlyEarningsCeiling: number;
  uifContributionRate: number;
  sdlRate: number;
  isActive: boolean;
  taxTables: TaxTable[];
  taxThresholds: TaxThreshold[];
  taxRebates: TaxRebate[];
}

export interface Company {
  id: string;
  name: string;
  registrationNumber: string;
  taxNumber?: string;
  uifNumber?: string;
  sdlNumber?: string;
  physicalAddress?: string;
  postalAddress?: string;
  phone?: string;
  email?: string;
  isActive: boolean;
  hasLogo: boolean;
}
