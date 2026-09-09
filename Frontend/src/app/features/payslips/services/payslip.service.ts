import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { EmailPayslipResult, Payslip, PayslipDetail, PayslipPeriodSummary } from '../models/payslip.models';

@Injectable({ providedIn: 'root' })
export class PayslipService {
  private url = `${environment.apiUrl}payslips`;

  constructor(private http: HttpClient) {}

  getPeriods(companyId: string) {
    return this.http.get<PayslipPeriodSummary[]>(`${this.url}/periods`, {
      params: new HttpParams().set('companyId', companyId)
    });
  }

  getForPeriod(periodId: string) {
    return this.http.get<Payslip[]>(`${this.url}/period/${periodId}`);
  }

  getMine() {
    return this.http.get<Payslip[]>(`${this.url}/me`);
  }

  getForEmployee(employeeId: string) {
    return this.http.get<Payslip[]>(`${this.url}/employee/${employeeId}`);
  }

  getDetail(payrollLineId: string) {
    return this.http.get<PayslipDetail>(`${this.url}/${payrollLineId}/detail`);
  }

  download(payrollLineId: string) {
    return this.http.get(`${this.url}/${payrollLineId}/download`, { responseType: 'blob' });
  }

  downloadPeriod(periodId: string) {
    return this.http.get(`${this.url}/period/${periodId}/download-all`, { responseType: 'blob' });
  }

  email(payrollLineId: string) {
    return this.http.post<EmailPayslipResult>(`${this.url}/${payrollLineId}/email`, null);
  }

  emailPeriod(periodId: string) {
    return this.http.post<EmailPayslipResult>(`${this.url}/period/${periodId}/email-all`, null);
  }

  saveBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
