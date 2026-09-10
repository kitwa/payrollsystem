import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../../employees/services/employee.service';
import { EmployeeList } from '../../../employees/models/employee.models';
import { PayrollService } from '../../../payroll/services/payroll.service';
import { PayrollPeriod, PayrollStatus } from '../../../payroll/models/payroll.models';
import { EmployeeBonusService } from '../../services/employee-bonus.service';
import { EmployeeBonus } from '../../models/employee-bonus.models';
import { SettingsService } from '../../services/settings.service';
import { EarningType } from '../../models/settings.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-employee-bonuses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  template: `
    <section class="mb-3">
      <h1 class="h3 mb-1">Employee Bonuses</h1>
      <p class="text-muted mb-0">Add one-off bonuses to an employee's selected payroll period.</p>
    </section>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }

    <section class="card border-0 shadow-sm mb-3">
      <div class="card-body">
        <form class="row g-3" [formGroup]="form" (ngSubmit)="add()">
          <div class="col-12 col-lg-3">
            <label class="form-label">Employee</label>
            <select class="form-select" formControlName="employeeId" (change)="loadBonuses()">
              <option value="">Select employee</option>
              @for (employee of employees(); track employee.id) {
                <option [value]="employee.id">{{ employee.firstName }} {{ employee.lastName }} · {{ employee.employeeNumber }}</option>
              }
            </select>
          </div>
          <div class="col-12 col-lg-3">
            <label class="form-label">Payroll period</label>
            <select class="form-select" formControlName="payrollPeriodId" (change)="loadBonuses()">
              <option value="">Select period</option>
              @for (period of payrollPeriods(); track period.id) {
                <option [value]="period.id">{{ periodName(period) }} · {{ statusName(period.status) }}</option>
              }
            </select>
          </div>
          <div class="col-12 col-md-5 col-lg-3">
            <label class="form-label">Payroll item</label>
            <select class="form-select" formControlName="earningTypeId">
              <option value="">Select earning type</option>
              @for (item of earningTypes(); track item.id) {
                <option [value]="item.id">{{ item.name }} ({{ item.code }})</option>
              }
            </select>
          </div>
          <div class="col-12 col-md-5 col-lg-3">
            <label class="form-label">Description</label>
            <input class="form-control" formControlName="description" placeholder="e.g. Performance bonus">
          </div>
          <div class="col-6 col-md-3 col-lg-2">
            <label class="form-label">Amount</label>
            <input class="form-control" type="number" min="0.01" step="0.01" formControlName="amount">
          </div>
          <div class="col-6 col-md-4 col-lg-1 d-flex align-items-end">
            <button class="btn btn-dark w-100" type="submit" [disabled]="form.invalid || !canEditPeriod()">Add</button>
          </div>
          <div class="col-12">
            @if (selectedPeriod() && !canEditPeriod()) {
              <small class="text-danger">Bonuses can only be added to draft or approved payroll periods.</small>
            }
          </div>
        </form>
      </div>
    </section>

    <section class="card border-0 shadow-sm">
      <div class="card-body">
        <h2 class="h5 mb-3">Configured Bonuses</h2>
        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>Description</th><th>Notes</th><th class="text-end">Amount</th><th class="text-end">Action</th></tr></thead>
            <tbody>
              @for (bonus of pagedBonuses(); track bonus.id) {
                <tr>
                  <td>{{ bonus.description }}</td>
                  <td>{{ bonus.notes || '—' }}</td>
                  <td class="text-end">R {{ bonus.amount | number:'1.2-2' }}</td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-danger" type="button" [disabled]="!canEditPeriod()" (click)="remove(bonus)">Delete</button></td>
                </tr>
              } @empty {
                <tr><td colspan="4" class="text-center text-muted py-4">Select an employee and payroll period to view bonuses.</td></tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="bonuses().length" (pageChange)="pageNumber.set($event)"></app-pagination>
      </div>
    </section>
  `
})
export class EmployeeBonusesComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly employeeService = inject(EmployeeService);
  private readonly payrollService = inject(PayrollService);
  private readonly bonusService = inject(EmployeeBonusService);
  private readonly settingsService = inject(SettingsService);

  readonly employees = signal<EmployeeList[]>([]);
  readonly payrollPeriods = signal<PayrollPeriod[]>([]);
  readonly bonuses = signal<EmployeeBonus[]>([]);
  readonly earningTypes = signal<EarningType[]>([]);
  readonly pageNumber = signal(1);
  readonly pageSize = 20;
  readonly pagedBonuses = computed(() => this.bonuses().slice((this.pageNumber() - 1) * this.pageSize, this.pageNumber() * this.pageSize));
  readonly selectedPeriod = signal<PayrollPeriod | null>(null);
  readonly error = signal('');
  readonly message = signal('');

  readonly form = this.fb.group({
    employeeId: ['', Validators.required],
    payrollPeriodId: ['', Validators.required],
    earningTypeId: ['', Validators.required],
    description: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  constructor() {
    const companyId = this.auth.companyId();
    if (!companyId) return;

    this.employeeService.getAll(companyId).subscribe({
      next: page => this.employees.set(page.items),
      error: response => this.showError(response, 'Unable to load employees.')
    });
    this.payrollService.getPeriods(companyId).subscribe({
      next: periods => this.payrollPeriods.set(periods),
      error: response => this.showError(response, 'Unable to load payroll periods.')
    });
    this.settingsService.getEarningTypes(companyId).subscribe({
      next: types => this.earningTypes.set(types.filter(type => type.isActive)),
      error: response => this.showError(response, 'Unable to load earning types.')
    });
  }

  loadBonuses(): void {
    const employeeId = this.form.controls.employeeId.value ?? '';
    const periodId = this.form.controls.payrollPeriodId.value ?? '';
    this.selectedPeriod.set(this.payrollPeriods().find(period => period.id === periodId) ?? null);
    this.pageNumber.set(1);
    const companyId = this.auth.companyId();
    if (!companyId || !employeeId || !periodId) {
      this.bonuses.set([]);
      return;
    }

    this.bonusService.getAll(companyId, periodId, employeeId).subscribe({
      next: bonuses => this.bonuses.set(bonuses),
      error: response => this.showError(response, 'Unable to load bonuses.')
    });
  }

  add(): void {
    const companyId = this.auth.companyId();
    if (!companyId || this.form.invalid || !this.canEditPeriod()) return;
    const value = this.form.getRawValue();
    this.clearFeedback();
    this.bonusService.create({
      companyId,
      employeeId: value.employeeId!,
      payrollPeriodId: value.payrollPeriodId!,
      earningTypeId: value.earningTypeId!,
      description: value.description!.trim(),
      amount: value.amount!,
      notes: value.notes?.trim() || undefined
    }).subscribe({
      next: () => {
        this.message.set('Employee bonus added.');
        this.form.patchValue({ description: '', amount: 0, notes: '' });
        this.loadBonuses();
      },
      error: response => this.showError(response, 'Unable to add bonus.')
    });
  }

  remove(bonus: EmployeeBonus): void {
    if (!this.canEditPeriod()) return;
    this.clearFeedback();
    this.bonusService.delete(bonus.id).subscribe({
      next: () => { this.message.set('Employee bonus deleted.'); this.loadBonuses(); },
      error: response => this.showError(response, 'Unable to delete bonus.')
    });
  }

  canEditPeriod(): boolean {
    const status = this.selectedPeriod()?.status;
    return status === PayrollStatus.Draft || status === PayrollStatus.Approved;
  }

  periodName(period: PayrollPeriod): string {
    return new Date(period.year, period.month - 1, 1).toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
  }

  statusName(status: PayrollStatus): string {
    return PayrollStatus[status];
  }

  private clearFeedback(): void { this.error.set(''); this.message.set(''); }
  private showError(response: { error?: { errors?: string[] } }, fallback: string): void { this.error.set(response.error?.errors?.[0] ?? fallback); }
}
