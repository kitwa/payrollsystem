import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { GeneratePayroll, PayrollLine, PayrollPeriod } from '../models/payroll.models';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private url = `${environment.apiUrl}payroll`;

  constructor(private http: HttpClient) {}

  getPeriods(companyId: string) {
    const params = new HttpParams().set('companyId', companyId);
    return this.http.get<PayrollPeriod[]>(`${this.url}/periods`, { params });
  }

  getById(periodId: string) {
    return this.http.get<PayrollPeriod>(`${this.url}/${periodId}`);
  }

  getLines(periodId: string) {
    return this.http.get<PayrollLine[]>(`${this.url}/${periodId}/lines`);
  }

  generate(dto: GeneratePayroll) {
    return this.http.post<string>(`${this.url}/generate`, dto);
  }

  approve(periodId: string) {
    return this.http.post(`${this.url}/${periodId}/approve`, null);
  }

  lock(periodId: string) {
    return this.http.post(`${this.url}/${periodId}/lock`, null);
  }

  markPaid(periodId: string) {
    return this.http.post(`${this.url}/${periodId}/mark-paid`, null);
  }

  delete(periodId: string) {
    return this.http.delete(`${this.url}/${periodId}`);
  }
}
