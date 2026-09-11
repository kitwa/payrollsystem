import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CreateEmployee, Department, Employee, EmployeeList } from '../models/employee.models';

interface EmployeePage {
  items: EmployeeList[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private url = `${environment.apiUrl}departments`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string) {
    return this.http.get<Department[]>(this.url, { params: new HttpParams().set('companyId', companyId) });
  }

  create(companyId: string, name: string) {
    return this.http.post<string>(this.url, { companyId, name });
  }

  update(id: string, name: string) {
    return this.http.put(`${this.url}/${id}`, { name });
  }

  delete(id: string) {
    return this.http.delete(`${this.url}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private url = `${environment.apiUrl}employees`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string, page = 1, pageSize = 20) {
    const params = new HttpParams().set('companyId', companyId).set('pageNumber', page).set('pageSize', pageSize);
    return this.http.get<EmployeePage>(this.url, { params });
  }

  getById(id: string) {
    return this.http.get<Employee>(`${this.url}/${id}`);
  }

  getMine() {
    return this.http.get<Employee>(`${this.url}/me`);
  }

  create(dto: CreateEmployee) {
    return this.http.post<string>(this.url, dto);
  }

  update(id: string, dto: Partial<Employee>) {
    return this.http.put(`${this.url}/${id}`, dto);
  }

  updateBankDetails(id: string, bankDetails: Employee['bankDetails']) {
    return this.http.put(`${this.url}/${id}/bank-details`, bankDetails);
  }

  terminate(id: string, terminationDate: string) {
    return this.http.post(`${this.url}/${id}/terminate`, null, {
      params: new HttpParams().set('terminationDate', terminationDate)
    });
  }
}
