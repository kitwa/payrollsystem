export interface TaxYearSummary { id: string; year: number; startDate: string; endDate: string; isActive: boolean; }
export interface TaxCertificate {
  id: string; employeeId: string; employeeName: string; companyId: string; taxYearId: string; taxYearName: string;
  certificateType: 'IRP5' | 'IT3A'; certificateNumber: string; status: number;
  grossRemuneration: number; taxableIncome: number; paye: number; uif: number; sdl: number;
  allowances: number; bonuses: number; benefits: number; generatedAt?: string; isFinal: boolean;
}
export interface TaxCertificateLine { id: string; sarsCode: string; description: string; amount: number; taxableAmount: number; }
export interface TaxCertificateDetail { certificate: TaxCertificate; lines: TaxCertificateLine[]; }