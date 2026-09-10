import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CreateUserRequest, ManagedUser } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly url = `${environment.apiUrl}users`;

  constructor(private http: HttpClient) {}

  getAll(companyId: string) {
    return this.http.get<ManagedUser[]>(this.url, { params: new HttpParams().set('companyId', companyId) });
  }

  create(request: CreateUserRequest) {
    return this.http.post<string>(this.url, request);
  }

  createSuperAdmin(request: { email: string; firstName: string; lastName: string; password: string }) {
    return this.http.post<string>(`${this.url}/super-admin`, request);
  }

  updateRoles(userId: string, roles: string[]) {
    return this.http.put(`${this.url}/${userId}/roles`, { roles });
  }

  updateStatus(userId: string, isActive: boolean) {
    return this.http.put(`${this.url}/${userId}/status`, { isActive });
  }
}
