import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CreateSupportTicketRequest, SupportTicketDetail, SupportTicketPage, SupportTicketStatus, SupportTicketType } from '../models/support-ticket.models';

@Injectable({ providedIn: 'root' })
export class SupportTicketService {
  private readonly url = `${environment.apiUrl}support/tickets`;

  constructor(private http: HttpClient) {}

  getAll(options: {
    companyId?: string;
    status?: SupportTicketStatus;
    type?: SupportTicketType;
    search?: string;
    pageNumber?: number;
    pageSize?: number;
  } = {}) {
    let params = new HttpParams()
      .set('pageNumber', options.pageNumber ?? 1)
      .set('pageSize', options.pageSize ?? 20);
    if (options.companyId) params = params.set('companyId', options.companyId);
    if (options.status !== undefined) params = params.set('status', options.status);
    if (options.type !== undefined) params = params.set('type', options.type);
    if (options.search?.trim()) params = params.set('search', options.search.trim());
    return this.http.get<SupportTicketPage>(this.url, { params });
  }

  get(id: string) {
    return this.http.get<SupportTicketDetail>(`${this.url}/${id}`);
  }

  create(request: CreateSupportTicketRequest) {
    return this.http.post<SupportTicketDetail>(this.url, request);
  }

  close(id: string) {
    return this.http.post<SupportTicketDetail>(`${this.url}/${id}/close`, null);
  }

  getContact() {
    return this.http.get<{ email: string }>(`${environment.apiUrl}support/contact`);
  }

  updateStatus(id: string, status: SupportTicketStatus) {
    return this.http.put<SupportTicketDetail>(`${this.url}/${id}/status`, { status });
  }
}
