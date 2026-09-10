import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminCompanyDto } from '../models/management.models';

@Injectable({ providedIn: 'root' })
export class AdminCompanyService {
  private readonly url = `${environment.apiUrl}admin/companies`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<AdminCompanyDto[]>(this.url);
  }

  disable(companyId: string) {
    return this.http.post<void>(`${this.url}/${companyId}/disable`, {});
  }

  enable(companyId: string) {
    return this.http.post<void>(`${this.url}/${companyId}/enable`, {});
  }

  delete(companyId: string) {
    return this.http.delete<void>(`${this.url}/${companyId}`);
  }
}
