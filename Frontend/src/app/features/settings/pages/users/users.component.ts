import { Component, computed, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../../employees/services/employee.service';
import { EmployeeList } from '../../../employees/models/employee.models';
import { SettingsService } from '../../services/settings.service';
import { Company } from '../../models/settings.models';
import { ManagedUser, CreateUserRequest } from '../../models/user.models';
import { UserService } from '../../services/user.service';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent, ConfirmDialogComponent],
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
            <div class="input-group">
              <input class="form-control" [type]="showPassword() ? 'text' : 'password'" formControlName="password" autocomplete="new-password">
              <button class="btn btn-outline-secondary" type="button" (click)="showPassword.set(!showPassword())"><i class="bi" [class.bi-eye]="!showPassword()" [class.bi-eye-slash]="showPassword()"></i></button>
            </div>
            @if (form.controls.password.value) {
              <ul class="list-unstyled small mt-2 mb-0">
                @for (check of passwordChecks(); track check.label) {
                  <li [class.text-success]="check.passed" [class.text-danger]="!check.passed">
                    <i class="bi" [class.bi-check-circle-fill]="check.passed" [class.bi-x-circle-fill]="!check.passed"></i> {{ check.label }}
                  </li>
                }
              </ul>
            }
          </div>
          <div class="col-12 col-md-3 col-lg-1 d-flex align-items-end">
            <button class="btn btn-dark w-100" type="submit" [disabled]="form.invalid || !selectedCompanyId() || !passwordValid()">Add</button>
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
              @for (user of pagedUsers(); track user.userId) {
                <tr>
                  <td><p class="mb-0 fw-semibold">{{ user.firstName }} {{ user.lastName }}</p><small class="text-muted">{{ user.email }}</small></td>
                  <td>{{ user.employeeName || 'Not linked' }}</td>
                  <td>
                    <select
                      class="form-select form-select-sm"
                      [value]="primaryRole(user)"
                      [disabled]="isCurrentUser(user)"
                      [title]="isCurrentUser(user) ? 'You cannot change your own role.' : 'Change user role'"
                      (change)="changeRole(user, $event)"
                    >
                      <option value="Employee">Employee</option>
                      <option value="PayrollManager">Payroll Manager</option>
                      @if (isSuperAdmin()) { <option value="Admin">Admin</option> }
                    </select>
                  </td>
                  <td><span class="badge" [class.bg-success]="user.isActive" [class.bg-secondary]="!user.isActive">{{ user.isActive ? 'Active' : 'Disabled' }}</span></td>
                  <td class="text-end">
                    <button class="btn btn-sm btn-outline-secondary me-2" type="button" (click)="toggleStatus(user)">{{ user.isActive ? 'Disable' : 'Enable' }}</button>
                    @if (!isCurrentUser(user)) {
                      <button class="btn btn-sm btn-outline-danger" type="button" (click)="deleteUser(user)">Delete</button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="text-center text-muted py-4">No user accounts found.</td></tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="users().length" (pageChange)="pageNumber.set($event)"></app-pagination>
      </div>
    </section>

    <app-confirm-dialog #confirmDialog></app-confirm-dialog>
  `
})
export class UsersComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly employeeService = inject(EmployeeService);
  private readonly settingsService = inject(SettingsService);

  @ViewChild('confirmDialog') confirmDialog!: ConfirmDialogComponent;

  readonly users = signal<ManagedUser[]>([]);
  readonly employees = signal<EmployeeList[]>([]);
  readonly companies = signal<Company[]>([]);
  readonly selectedCompanyId = signal<string | null>(this.auth.companyId());
  readonly showPassword = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly pageNumber = signal(1);
  readonly pageSize = 20;
  readonly pagedUsers = computed(() => this.users().slice((this.pageNumber() - 1) * this.pageSize, this.pageNumber() * this.pageSize));

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

  readonly passwordChecks = computed(() => {
    const password = this.form.controls.password.value ?? '';
    return [
      { label: 'At least 8 characters', passed: password.length >= 8 },
      { label: 'One uppercase letter', passed: /[A-Z]/.test(password) },
      { label: 'One lowercase letter', passed: /[a-z]/.test(password) },
      { label: 'One number', passed: /\d/.test(password) },
      { label: 'One special character', passed: /[^A-Za-z0-9]/.test(password) }
    ];
  });

  readonly passwordValid = computed(() => this.passwordChecks().every(check => check.passed));

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
    this.pageNumber.set(1);
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

  isCurrentUser(user: ManagedUser): boolean {
    return user.userId === this.auth.currentUser()?.userId;
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

  async deleteUser(user: ManagedUser): Promise<void> {
    const confirmed = await this.confirmDialog.show({
      title: 'Delete user account',
      message: `Delete the account for ${user.firstName} ${user.lastName}? This cannot be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger'
    });
    if (!confirmed) return;

    this.clearFeedback();
    this.userService.delete(user.userId).subscribe({
      next: () => { this.message.set('User account deleted.'); this.load(); },
      error: response => this.showError(response, 'Unable to delete user account.')
    });
  }

  private clearFeedback(): void { this.error.set(''); this.message.set(''); }

  private showError(response: { error?: { errors?: string[] } }, fallback: string): void {
    this.error.set(response.error?.errors?.[0] ?? fallback);
  }
}
