import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private url = `${environment.apiUrl}reports`;

  constructor(private http: HttpClient) {}

  payrollRegister(periodId: string) {
    return this.http.get<any>(`${this.url}/payroll-register`, { params: new HttpParams().set('periodId', periodId) });
  }

  leaveReport(companyId: string, from: string, to: string) {
    return this.http.get<any[]>(`${this.url}/leave-report`, { params: new HttpParams().set('companyId', companyId).set('from', from).set('to', to) });
  }

  taxReport(periodId: string) {
    return this.http.get<any>(`${this.url}/tax-report`, { params: new HttpParams().set('periodId', periodId) });
  }

  employeeCost(companyId: string, year: number) {
    return this.http.get<any>(`${this.url}/employee-cost`, { params: new HttpParams().set('companyId', companyId).set('year', year) });
  }
}
