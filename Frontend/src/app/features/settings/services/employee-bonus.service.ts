import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CreateEmployeeBonus, EmployeeBonus } from '../models/employee-bonus.models';

@Injectable({ providedIn: 'root' })
export class EmployeeBonusService {
  private readonly url = `${environment.apiUrl}employee-bonuses`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string, payrollPeriodId: string, employeeId: string) {
    const params = new HttpParams()
      .set('companyId', companyId)
      .set('payrollPeriodId', payrollPeriodId)
      .set('employeeId', employeeId);
    return this.http.get<EmployeeBonus[]>(this.url, { params });
  }

  create(dto: CreateEmployeeBonus) {
    return this.http.post<string>(this.url, dto);
  }

  delete(id: string) {
    return this.http.delete(`${this.url}/${id}`);
  }
}
