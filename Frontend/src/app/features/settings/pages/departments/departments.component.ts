import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { DepartmentService } from '../../../employees/services/employee.service';
import { Department } from '../../../employees/models/employee.models';
import { SettingsService } from '../../services/settings.service';
import { Company } from '../../models/settings.models';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
      <div>
        <h1 class="h3 mb-1">Departments</h1>
        <p class="text-muted mb-0">Manage the departments available on employee profiles.</p>
      </div>
    </section>

    <section class="card border-0 shadow-sm">
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
        <form class="row g-2 mb-3" [formGroup]="form" (ngSubmit)="save()">
          <div class="col-12 col-md-8">
            <label class="form-label">Department name</label>
            <input class="form-control" formControlName="name" placeholder="e.g. Finance">
          </div>
          <div class="col-12 col-md-4 d-flex align-items-end gap-2">
            <button class="btn btn-dark" type="submit" [disabled]="form.invalid">{{ editingId() ? 'Save Changes' : 'Add Department' }}</button>
            @if (editingId()) {
              <button class="btn btn-outline-secondary" type="button" (click)="cancelEdit()">Cancel</button>
            }
          </div>
        </form>

        @if (error()) {
          <div class="alert alert-danger">{{ error() }}</div>
        }

        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>Department</th><th class="text-end">Actions</th></tr></thead>
            <tbody>
              @for (department of departments(); track department.id) {
                <tr>
                  <td>{{ department.name }}</td>
                  <td class="text-end">
                    <button class="btn btn-sm btn-outline-dark me-2" type="button" (click)="edit(department)">Edit</button>
                    <button class="btn btn-sm btn-outline-danger" type="button" (click)="remove(department)">Delete</button>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="2" class="text-center text-muted py-4">No departments configured.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </section>
  `
})
export class DepartmentsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly departmentService = inject(DepartmentService);
  private readonly settingsService = inject(SettingsService);

  readonly departments = signal<Department[]>([]);
  readonly companies = signal<Company[]>([]);
  readonly selectedCompanyId = signal<string | null>(this.auth.companyId());
  readonly editingId = signal<string | null>(null);
  readonly error = signal('');
  readonly form = this.fb.group({ name: ['', Validators.required] });

  readonly canSelectCompany = () => !this.auth.companyId() && this.auth.isInRole('SuperAdmin');

  constructor() {
    if (this.canSelectCompany()) {
      this.settingsService.getCompanies().subscribe({
        next: companies => {
          this.companies.set(companies);
          if (!this.selectedCompanyId() && companies.length) {
            this.selectedCompanyId.set(companies[0].id);
            this.load();
          }
        },
        error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load companies.')
      });
    } else {
      this.load();
    }
  }

  private load(): void {
    const companyId = this.selectedCompanyId();
    if (companyId) this.departmentService.getAll(companyId).subscribe(items => this.departments.set(items));
  }

  selectCompany(event: Event): void {
    const companyId = (event.target as HTMLSelectElement).value || null;
    this.selectedCompanyId.set(companyId);
    this.departments.set([]);
    this.cancelEdit();
    if (companyId) this.load();
  }

  save(): void {
    const companyId = this.selectedCompanyId();
    const name = this.form.controls.name.value?.trim();
    if (!companyId) {
      this.error.set('Select a company before adding a department.');
      return;
    }
    if (!name) return;
    this.error.set('');

    const request = this.editingId()
      ? this.departmentService.update(this.editingId()!, name)
      : this.departmentService.create(companyId, name);

    request.subscribe({
      next: () => { this.form.reset(); this.editingId.set(null); this.load(); },
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to save department.')
    });
  }

  edit(department: Department): void {
    this.editingId.set(department.id);
    this.form.setValue({ name: department.name });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset();
    this.error.set('');
  }

  remove(department: Department): void {
    this.error.set('');
    this.departmentService.delete(department.id).subscribe({
      next: () => this.load(),
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to delete department.')
    });
  }
}
