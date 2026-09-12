import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthResponse, CurrentUser, RegisterCompanyRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'payrollsa_user';

  currentUser = signal<CurrentUser | null>(this.loadFromStorage());
  isLoggedIn = computed(() => !!this.currentUser());
  roles = computed(() => this.currentUser()?.roles ?? []);
  companyId = computed(() => this.currentUser()?.companyId ?? null);
  employeeId = computed(() => this.currentUser()?.employeeId ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}auth/login`, { email, password }).pipe(
      tap(response => this.setCurrentUser(response))
    );
  }

  forgotPassword(email: string) {
    return this.http.post<void>(`${environment.apiUrl}auth/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.http.post<void>(`${environment.apiUrl}auth/reset-password`, { email, token, newPassword });
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<void>(`${environment.apiUrl}auth/change-password`, { currentPassword, newPassword });
  }

  registerCompany(request: RegisterCompanyRequest) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}auth/register-company`, request).pipe(
      tap(response => this.setCurrentUser(response))
    );
  }

  refreshToken() {
    const user = this.currentUser();
    if (!user) return;
    return this.http.post<AuthResponse>(`${environment.apiUrl}auth/refresh-token`, { refreshToken: user.refreshToken }).pipe(
      tap(response => this.setCurrentUser(response))
    );
  }

  logout() {
    localStorage.removeItem(this.storageKey);
    this.currentUser.set(null);
    this.router.navigateByUrl('/login');
  }

  isInRole(role: string): boolean {
    return this.roles().includes(role);
  }

  private setCurrentUser(response: AuthResponse) {
    const user: CurrentUser = {
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      roles: response.roles,
      token: response.accessToken,
      refreshToken: response.refreshToken,
      userId: response.userId,
      companyId: response.companyId,
      employeeId: response.employeeId
    };
    localStorage.setItem(this.storageKey, JSON.stringify(user));
    this.currentUser.set(user);
  }

  private loadFromStorage(): CurrentUser | null {
    const stored = localStorage.getItem(this.storageKey);
    return stored ? JSON.parse(stored) : null;
  }
}
