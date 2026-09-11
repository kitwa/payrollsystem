import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { TaxCertificate, TaxCertificateDetail, TaxYearSummary } from '../models/tax-certificate.models';

@Injectable({ providedIn: 'root' })
export class TaxCertificateService {
  private readonly url = `${environment.apiUrl}tax-certificates`;
  private readonly taxUrl = `${environment.apiUrl}tax`;
  constructor(private http: HttpClient) {}
  getYears() { return this.http.get<TaxYearSummary[]>(`${this.taxUrl}/years`); }
  getAll(companyId: string, taxYearId: string, search = '', status = '', type = '') {
    let params = new HttpParams().set('companyId', companyId).set('taxYearId', taxYearId);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    if (type) params = params.set('type', type);
    return this.http.get<TaxCertificate[]>(this.url, { params });
  }
  generate(companyId: string, employeeId: string, taxYearId: string, certificateType = 0) {
    return this.http.post<string>(`${this.url}/generate`, { companyId, employeeId, taxYearId, certificateType });
  }
  generateAll(companyId: string, taxYearId: string, certificateType = 0) {
    return this.http.post<number>(`${this.url}/generate-all`, { companyId, taxYearId, certificateType });
  }
  getById(id: string) { return this.http.get<TaxCertificateDetail>(`${this.url}/${id}`); }
  download(id: string) { return this.http.get(`${this.url}/${id}/download`, { responseType: 'blob' }); }
  finalize(id: string) { return this.http.post<void>(`${this.url}/${id}/finalize`, {}); }
  downloadUrl(id: string) { return `${this.url}/${id}/download`; }
}