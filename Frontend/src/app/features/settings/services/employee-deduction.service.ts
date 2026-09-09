import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { EmployeeDeduction } from '../models/employee-deduction.models';

@Injectable({ providedIn: 'root' })
export class EmployeeDeductionService {
  private readonly url = `${environment.apiUrl}employee-deductions`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string, employeeId: string) {
    const params = new HttpParams().set('companyId', companyId).set('employeeId', employeeId);
    return this.http.get<EmployeeDeduction[]>(this.url, { params });
  }

  create(dto: { companyId: string; employeeId: string; description: string; category: number; employeeAmount: number; employerAmount: number }) {
    return this.http.post<string>(this.url, dto);
  }

  update(id: string, dto: Partial<EmployeeDeduction>) {
    return this.http.put(`${this.url}/${id}`, dto);
  }
}
