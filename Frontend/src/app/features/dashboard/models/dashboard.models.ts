export interface DashboardSummary {
  totalEmployees: number;
  activeEmployees: number;
  onLeaveEmployees: number;
  pendingLeaveRequests: number;
  currentPeriodId?: string | null;
  currentPeriodStatus?: string | null;
  currentPeriodEmployeeCount: number;
  currentPeriodTotalNet: number;
  ytdGross: number;
  ytdDeductions: number;
  ytdNet: number;
}
