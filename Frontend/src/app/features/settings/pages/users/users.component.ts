import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../../employees/services/employee.service';
import { EmployeeList } from '../../../employees/models/employee.models';
import { SettingsService } from '../../services/settings.service';
import { Company } from '../../models/settings.models';
import { ManagedUser, CreateUserRequest } from '../../models/user.models';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="mb-3">
      <h1 class="h3 mb-1">User Accounts</h1>
      <p class="text-muted mb-0">Create employee logins and manage company roles.</p>
    </section>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }

    <section class="card border-0 shadow-sm mb-3">
      <div class="card-body">
        @if (canSelectCompany()) {
          <div class="mb-3">
            <label class="form-label">Company</label>
            <select class="form-select" [value]="selectedCompanyId() ?? ''" (change)="selectCompany($event)">
              <option value="">Select company</option>
              @for (company of companies(); track company.id) {
                <option [value]="company.id">{{ company.name }}</option>
              }
            </select>
          </div>
        }

        <h2 class="h5 mb-3">Create Employee Login</h2>
        <form class="row g-3" [formGroup]="form" (ngSubmit)="create()">
          <div class="col-12 col-lg-5">
            <label class="form-label">Employee</label>
            <select class="form-select" formControlName="employeeId">
              <option value="">Select employee</option>
              @for (employee of availableEmployees(); track employee.id) {
                <option [value]="employee.id">{{ employee.firstName }} {{ employee.lastName }} · {{ employee.email }}</option>
              }
            </select>
          </div>
          <div class="col-12 col-md-4 col-lg-3">
            <label class="form-label">Role</label>
            <select class="form-select" formControlName="role">
              <option value="Employee">Employee</option>
              <option value="PayrollManager">Payroll Manager</option>
              @if (isSuperAdmin()) { <option value="Admin">Admin</option> }
            </select>
          </div>
          <div class="col-12 col-md-5 col-lg-3">
            <label class="form-label">Temporary Password</label>
            <input class="form-control" type="password" formControlName="password" autocomplete="new-password">
          </div>
          <div class="col-12 col-md-3 col-lg-1 d-flex align-items-end">
            <button class="btn btn-dark w-100" type="submit" [disabled]="form.invalid || !selectedCompanyId()">Add</button>
          </div>
        </form>
        <small class="text-muted d-block mt-2">The employee must have an email address. They can use this password to sign in.</small>
      </div>
    </section>

    <section class="card border-0 shadow-sm">
      <div class="card-body">
        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>User</th><th>Employee</th><th>Roles</th><th>Status</th><th class="text-end">Actions</th></tr></thead>
            <tbody>
              @for (user of users(); track user.userId) {
                <tr>
                  <td><p class="mb-0 fw-semibold">{{ user.firstName }} {{ user.lastName }}</p><small class="text-muted">{{ user.email }}</small></td>
                  <td>{{ user.employeeName || 'Not linked' }}</td>
                  <td>
                    <select class="form-select form-select-sm" [value]="primaryRole(user)" (change)="changeRole(user, $event)">
                      <option value="Employee">Employee</option>
                      <option value="PayrollManager">Payroll Manager</option>
                      @if (isSuperAdmin()) { <option value="Admin">Admin</option> }
                    </select>
                  </td>
                  <td><span class="badge" [class.bg-success]="user.isActive" [class.bg-secondary]="!user.isActive">{{ user.isActive ? 'Active' : 'Disabled' }}</span></td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-secondary" type="button" (click)="toggleStatus(user)">{{ user.isActive ? 'Disable' : 'Enable' }}</button></td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="text-center text-muted py-4">No user accounts found.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </section>
  `
})
export class UsersComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly employeeService = inject(EmployeeService);
  private readonly settingsService = inject(SettingsService);

  readonly users = signal<ManagedUser[]>([]);
  readonly employees = signal<EmployeeList[]>([]);
  readonly companies = signal<Company[]>([]);
  readonly selectedCompanyId = signal<string | null>(this.auth.companyId());
  readonly error = signal('');
  readonly message = signal('');

  readonly form = this.fb.group({
    employeeId: ['', Validators.required],
    role: ['Employee', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  readonly isSuperAdmin = computed(() => this.auth.isInRole('SuperAdmin'));
  readonly canSelectCompany = computed(() => !this.auth.companyId() && this.isSuperAdmin());
  readonly availableEmployees = computed(() => {
    const linked = new Set(this.users().map(user => user.employeeId).filter(Boolean));
    return this.employees().filter(employee => !linked.has(employee.id));
  });

  constructor() {
    if (this.canSelectCompany()) {
      this.settingsService.getCompanies().subscribe({
        next: companies => {
          this.companies.set(companies);
          if (companies.length) {
            this.selectedCompanyId.set(companies[0].id);
            this.load();
          }
        },
        error: response => this.showError(response, 'Unable to load companies.')
      });
    } else {
      this.load();
    }
  }

  private load(): void {
    const companyId = this.selectedCompanyId();
    if (!companyId) return;
    this.employeeService.getAll(companyId).subscribe({
      next: page => this.employees.set(page.items),
      error: response => this.showError(response, 'Unable to load employees.')
    });
    this.userService.getAll(companyId).subscribe({
      next: users => this.users.set(users),
      error: response => this.showError(response, 'Unable to load user accounts.')
    });
  }

  selectCompany(event: Event): void {
    this.selectedCompanyId.set((event.target as HTMLSelectElement).value || null);
    this.users.set([]);
    this.employees.set([]);
    this.form.controls.employeeId.reset('');
    this.load();
  }

  create(): void {
    const companyId = this.selectedCompanyId();
    if (!companyId || this.form.invalid) return;
    const value = this.form.getRawValue();
    const request: CreateUserRequest = {
      companyId,
      employeeId: value.employeeId!,
      password: value.password!,
      role: value.role as CreateUserRequest['role']
    };

    this.clearFeedback();
    this.userService.create(request).subscribe({
      next: () => { this.message.set('User account created.'); this.form.reset({ employeeId: '', role: 'Employee', password: '' }); this.load(); },
      error: response => this.showError(response, 'Unable to create user account.')
    });
  }

  primaryRole(user: ManagedUser): string {
    return user.roles.includes('Admin') ? 'Admin' : user.roles.includes('PayrollManager') ? 'PayrollManager' : 'Employee';
  }

  changeRole(user: ManagedUser, event: Event): void {
    const role = (event.target as HTMLSelectElement).value;
    this.clearFeedback();
    this.userService.updateRoles(user.userId, [role]).subscribe({
      next: () => { this.message.set('User role updated.'); this.load(); },
      error: response => this.showError(response, 'Unable to update user role.')
    });
  }

  toggleStatus(user: ManagedUser): void {
    this.clearFeedback();
    this.userService.updateStatus(user.userId, !user.isActive).subscribe({
      next: () => { this.message.set('User status updated.'); this.load(); },
      error: response => this.showError(response, 'Unable to update user status.')
    });
  }

  private clearFeedback(): void { this.error.set(''); this.message.set(''); }

  private showError(response: { error?: { errors?: string[] } }, fallback: string): void {
    this.error.set(response.error?.errors?.[0] ?? fallback);
  }
}
