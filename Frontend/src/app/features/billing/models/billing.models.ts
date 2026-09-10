export interface PlanDto {
  code: string;
  name: string;
  minEmployees: number;
  maxEmployees?: number | null;
  monthlyPrice?: number | null;
  isContactSales: boolean;
}

export interface SubscriptionSummaryDto {
  companyId: string;
  status: string;
  planCode: string;
  planName: string;
  minEmployees: number;
  maxEmployees?: number | null;
  monthlyPrice?: number | null;
  isContactSales: boolean;
  currentEmployeeCount: number;
  trialStartDate?: string | null;
  trialEndDate?: string | null;
  currentPeriodStart?: string | null;
  currentPeriodEnd?: string | null;
  canAddEmployee: boolean;
  recommendedPlanCode?: string | null;
}

export interface UpgradeSubscriptionDto {
  planCode: string;
}
