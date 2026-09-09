import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import {
  Company, CreateLeaveType, DeductionType, EarningType, LeaveType, TaxRebate, TaxTable, TaxThreshold, TaxYearDetail, UpdateLeaveType
} from '../models/settings.models';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private settingsUrl = `${environment.apiUrl}settings`;
  private taxUrl = `${environment.apiUrl}tax`;
  private companiesUrl = `${environment.apiUrl}companies`;

  constructor(private http: HttpClient) {}

  getLeaveTypes(companyId: string) {
    return this.http.get<LeaveType[]>(`${this.settingsUrl}/leave-types`, { params: new HttpParams().set('companyId', companyId) });
  }

  createLeaveType(dto: CreateLeaveType) {
    return this.http.post<string>(`${this.settingsUrl}/leave-types`, dto);
  }

  updateLeaveType(id: string, dto: UpdateLeaveType) {
    return this.http.put(`${this.settingsUrl}/leave-types/${id}`, dto);
  }

  getEarningTypes(companyId: string) {
    return this.http.get<EarningType[]>(`${this.settingsUrl}/earning-types`, { params: new HttpParams().set('companyId', companyId) });
  }

  getDeductionTypes(companyId: string) {
    return this.http.get<DeductionType[]>(`${this.settingsUrl}/deduction-types`, { params: new HttpParams().set('companyId', companyId) });
  }

  getActiveTaxYear() {
    return this.http.get<TaxYearDetail>(`${this.taxUrl}/year`);
  }

  updateTaxTable(id: string, dto: Partial<TaxTable>) {
    return this.http.put(`${this.taxUrl}/tables/${id}`, dto);
  }

  updateTaxThreshold(id: string, dto: Partial<TaxThreshold>) {
    return this.http.put(`${this.taxUrl}/thresholds/${id}`, dto);
  }

  updateTaxRebate(id: string, dto: Partial<TaxRebate>) {
    return this.http.put(`${this.taxUrl}/rebates/${id}`, dto);
  }

  getCompany(id: string) {
    return this.http.get<Company>(`${this.companiesUrl}/${id}`);
  }

  updateCompany(id: string, dto: Partial<Company>) {
    return this.http.put(`${this.companiesUrl}/${id}`, dto);
  }

  logoUrl(companyId: string): string {
    return `${this.companiesUrl}/${companyId}/logo`;
  }

  uploadLogo(companyId: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.companiesUrl}/${companyId}/logo`, formData);
  }

  removeLogo(companyId: string) {
    return this.http.delete(`${this.companiesUrl}/${companyId}/logo`);
  }

  getCompanies() {
    return this.http.get<Company[]>(this.companiesUrl);
  }
}
