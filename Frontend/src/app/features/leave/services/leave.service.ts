import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CreateLeaveRequest, EmployeeLeaveBalance, LeaveBalance, LeaveRequest } from '../models/leave.models';

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private url = `${environment.apiUrl}leave`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string, employeeId?: string) {
    let params = new HttpParams().set('companyId', companyId);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<LeaveRequest[]>(this.url, { params });
  }

  getById(id: string) {
    return this.http.get<LeaveRequest>(`${this.url}/${id}`);
  }

  getBalances(employeeId: string, year?: number) {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.http.get<LeaveBalance[]>(`${this.url}/balances/${employeeId}`, { params });
  }

  getMyBalances(year?: number) {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.http.get<LeaveBalance[]>(`${this.url}/balances/me`, { params });
  }

  getCompanyBalances(companyId: string, year?: number) {
    let params = new HttpParams().set('companyId', companyId);
    if (year) params = params.set('year', year);
    return this.http.get<EmployeeLeaveBalance[]>(`${this.url}/balances/company`, { params });
  }

  request(dto: CreateLeaveRequest) {
    return this.http.post<string>(`${this.url}/request`, dto);
  }

  approve(id: string) {
    return this.http.put(`${this.url}/${id}/approve`, null);
  }

  reject(id: string, note: string) {
    return this.http.put(`${this.url}/${id}/reject`, null, { params: new HttpParams().set('note', note) });
  }
}
