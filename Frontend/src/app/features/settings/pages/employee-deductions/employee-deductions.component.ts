import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../../employees/services/employee.service';
import { EmployeeList } from '../../../employees/models/employee.models';
import { EmployeeDeductionService } from '../../services/employee-deduction.service';
import { DeductionCategory, EmployeeDeduction } from '../../models/employee-deduction.models';

@Component({
  selector: 'app-employee-deductions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="mb-3">
      <h1 class="h3 mb-1">Employee Deductions</h1>
      <p class="text-muted mb-0">Add recurring custom deductions to an employee's payroll.</p>
    </section>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }

    <section class="card border-0 shadow-sm mb-3">
      <div class="card-body">
        <form class="row g-3" [formGroup]="form" (ngSubmit)="add()">
          <div class="col-12 col-lg-4">
            <label class="form-label">Employee</label>
            <select class="form-select" formControlName="employeeId" (change)="loadDeductions()">
              <option value="">Select employee</option>
              @for (employee of employees(); track employee.id) {
                <option [value]="employee.id">{{ employee.firstName }} {{ employee.lastName }} · {{ employee.employeeNumber }}</option>
              }
            </select>
          </div>
          <div class="col-12 col-md-6 col-lg-3">
            <label class="form-label">Description</label>
            <input class="form-control" formControlName="description" placeholder="e.g. Staff loan">
          </div>
          <div class="col-12 col-md-6 col-lg-2">
            <label class="form-label">Category</label>
            <select class="form-select" formControlName="category">
              @for (category of categories; track category.value) {
                <option [ngValue]="category.value">{{ category.label }}</option>
              }
            </select>
          </div>
          <div class="col-6 col-lg-1">
            <label class="form-label">Employee</label>
            <input class="form-control" type="number" min="0" formControlName="employeeAmount">
          </div>
          <div class="col-6 col-lg-1">
            <label class="form-label">Employer</label>
            <input class="form-control" type="number" min="0" formControlName="employerAmount">
          </div>
          <div class="col-12 col-lg-1 d-flex align-items-end">
            <button class="btn btn-dark w-100" type="submit" [disabled]="form.invalid || !selectedEmployeeId()">Add</button>
          </div>
        </form>
      </div>
    </section>

    <section class="card border-0 shadow-sm">
      <div class="card-body">
        <h2 class="h5 mb-3">Configured Deductions</h2>
        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>Description</th><th>Category</th><th class="text-end">Employee</th><th class="text-end">Employer</th><th>Status</th><th class="text-end">Action</th></tr></thead>
            <tbody>
              @for (deduction of deductions(); track deduction.id) {
                <tr>
                  <td>{{ deduction.description }}</td>
                  <td>{{ categoryName(deduction.category) }}</td>
                  <td class="text-end">R {{ deduction.employeeAmount | number:'1.2-2' }}</td>
                  <td class="text-end">R {{ deduction.employerAmount | number:'1.2-2' }}</td>
                  <td><span class="badge" [class.bg-success]="deduction.isActive" [class.bg-secondary]="!deduction.isActive">{{ deduction.isActive ? 'Active' : 'Disabled' }}</span></td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-secondary" type="button" (click)="toggle(deduction)">{{ deduction.isActive ? 'Disable' : 'Enable' }}</button></td>
                </tr>
              } @empty {
                <tr><td colspan="6" class="text-center text-muted py-4">Select an employee to view deductions.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </section>
  `
})
export class EmployeeDeductionsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly employeeService = inject(EmployeeService);
  private readonly deductionService = inject(EmployeeDeductionService);

  readonly employees = signal<EmployeeList[]>([]);
  readonly deductions = signal<EmployeeDeduction[]>([]);
  readonly error = signal('');
  readonly message = signal('');
  readonly selectedEmployeeId = signal('');

  readonly categories = [
    { value: DeductionCategory.Other, label: 'Other' },
    { value: DeductionCategory.Loan, label: 'Loan' },
    { value: DeductionCategory.Advance, label: 'Advance' },
    { value: DeductionCategory.MedicalAid, label: 'Medical Aid' },
    { value: DeductionCategory.Pension, label: 'Pension' },
    { value: DeductionCategory.ProvidentFund, label: 'Provident Fund' }
  ];

  readonly form = this.fb.group({
    employeeId: ['', Validators.required],
    description: ['', Validators.required],
    category: [DeductionCategory.Other, Validators.required],
    employeeAmount: [0, [Validators.required, Validators.min(0)]],
    employerAmount: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    const companyId = this.auth.companyId();
    if (companyId) {
      this.employeeService.getAll(companyId).subscribe({
        next: page => this.employees.set(page.items),
        error: response => this.showError(response, 'Unable to load employees.')
      });
    }
  }

  loadDeductions(): void {
    const employeeId = this.form.controls.employeeId.value ?? '';
    this.selectedEmployeeId.set(employeeId);
    const companyId = this.auth.companyId();
    if (companyId && employeeId) {
      this.deductionService.getAll(companyId, employeeId).subscribe({
        next: deductions => this.deductions.set(deductions),
        error: response => this.showError(response, 'Unable to load deductions.')
      });
    } else {
      this.deductions.set([]);
    }
  }

  add(): void {
    const companyId = this.auth.companyId();
    if (!companyId || this.form.invalid) return;
    const value = this.form.getRawValue();
    this.clearFeedback();
    this.deductionService.create({
      companyId,
      employeeId: value.employeeId!,
      description: value.description!,
      category: value.category!,
      employeeAmount: value.employeeAmount!,
      employerAmount: value.employerAmount!
    }).subscribe({
      next: () => { this.message.set('Employee deduction added.'); this.form.patchValue({ description: '', employeeAmount: 0, employerAmount: 0 }); this.loadDeductions(); },
      error: response => this.showError(response, 'Unable to add deduction.')
    });
  }

  toggle(deduction: EmployeeDeduction): void {
    this.deductionService.update(deduction.id, { ...deduction, isActive: !deduction.isActive }).subscribe({
      next: () => this.loadDeductions(),
      error: response => this.showError(response, 'Unable to update deduction.')
    });
  }

  categoryName(category: DeductionCategory): string {
    return DeductionCategory[category] ?? 'Other';
  }

  private clearFeedback(): void { this.error.set(''); this.message.set(''); }
  private showError(response: { error?: { errors?: string[] } }, fallback: string): void { this.error.set(response.error?.errors?.[0] ?? fallback); }
}
