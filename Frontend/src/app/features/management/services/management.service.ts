import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CompanyManagementSummary, ManagementAuditPage } from '../models/management.models';

@Injectable({ providedIn: 'root' })
export class ManagementService {
  private readonly url = `${environment.apiUrl}management`;

  constructor(private http: HttpClient) {}

  getSummary(companyId: string) {
    return this.http.get<CompanyManagementSummary>(`${this.url}/summary`, {
      params: new HttpParams().set('companyId', companyId)
    });
  }

  getAudit(companyId: string | undefined, pageNumber = 1, pageSize = 20) {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (companyId) params = params.set('companyId', companyId);
    return this.http.get<ManagementAuditPage>(`${this.url}/audit`, { params });
  }
}
