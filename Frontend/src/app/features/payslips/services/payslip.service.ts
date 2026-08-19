import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { Payslip } from '../models/payslip.models';

@Injectable({ providedIn: 'root' })
export class PayslipService {
  private url = `${environment.apiUrl}payslips`;

  constructor(private http: HttpClient) {}

  getForEmployee(employeeId: string) {
    return this.http.get<Payslip[]>(`${this.url}/${employeeId}`);
  }

  download(payrollLineId: string) {
    return this.http.get(`${this.url}/${payrollLineId}/download`, { responseType: 'blob' });
  }
}
