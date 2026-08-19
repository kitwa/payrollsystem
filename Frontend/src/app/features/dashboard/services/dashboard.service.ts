import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { DashboardSummary } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private url = `${environment.apiUrl}dashboard`;

  constructor(private http: HttpClient) {}

  getSummary(companyId: string) {
    return this.http.get<DashboardSummary>(`${this.url}/summary`, { params: new HttpParams().set('companyId', companyId) });
  }
}
