import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CreateEmployee, Employee, EmployeeList } from '../models/employee.models';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private url = `${environment.apiUrl}employees`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string, page = 1, pageSize = 20) {
    const params = new HttpParams().set('companyId', companyId).set('pageNumber', page).set('pageSize', pageSize);
    return this.http.get<EmployeeList[]>(this.url, { params });
  }

  getById(id: string) {
    return this.http.get<Employee>(`${this.url}/${id}`);
  }

  create(dto: CreateEmployee) {
    return this.http.post<string>(this.url, dto);
  }

  update(id: string, dto: Partial<Employee>) {
    return this.http.put(`${this.url}/${id}`, dto);
  }

  terminate(id: string, terminationDate: string) {
    return this.http.post(`${this.url}/${id}/terminate`, null, {
      params: new HttpParams().set('terminationDate', terminationDate)
    });
  }
}
