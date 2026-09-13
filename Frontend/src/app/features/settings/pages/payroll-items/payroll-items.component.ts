import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { SettingsService } from '../../services/settings.service';
import { DeductionType, EarningType } from '../../models/settings.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-payroll-items',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  template: `
    <section class="mb-3">
      <h1 class="h3 mb-1">Add Bonus / Deduction Types</h1>
      <p class="text-muted mb-0">Configure earning and deduction types used in payroll runs and payslips.</p>
    </section>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    <div class="row g-3">
      <div class="col-12 col-xl-6">
        <section class="card border-0 shadow-sm h-100">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <h2 class="h5 mb-0">Bonuses</h2>
            </div>

            <form class="row g-2 mb-3" [formGroup]="earningForm" (ngSubmit)="addEarningType()">
              <div class="col-12 col-md-6">
                <label class="form-label">Name</label>
                <input class="form-control" formControlName="name" placeholder="e.g. Bonus">
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label">Code</label>
                <input class="form-control" formControlName="code" placeholder="BONUS">
              </div>
              <div class="col-12 col-md-3 d-flex align-items-end">
                <div class="form-check form-switch mt-3">
                  <input class="form-check-input" type="checkbox" formControlName="isTaxable">
                  <label class="form-check-label">Taxable</label>
                </div>
              </div>
              <div class="col-12 d-flex justify-content-end">
                <button class="btn btn-dark" type="submit" [disabled]="earningForm.invalid">Add Bonus</button>
              </div>
            </form>

            <div class="table-responsive">
              <table class="table align-middle mb-0">
                <thead>
                  <tr><th>Name</th><th>Code</th><th>Taxable</th><th>Status</th><th></th></tr>
                </thead>
                <tbody>
                  @for (item of pagedEarningTypes(); track item.id) {
                    <tr>
                      <td>{{ item.name }}</td>
                      <td>{{ item.code }}</td>
                      <td>{{ item.isTaxable ? 'Yes' : 'No' }}</td>
                      <td><span class="badge" [class.bg-success]="item.isActive" [class.bg-secondary]="!item.isActive">{{ item.isActive ? 'Active' : 'Disabled' }}</span></td>
                      <td class="text-end"><button class="btn btn-sm btn-outline-secondary" type="button" (click)="toggleEarning(item)">{{ item.isActive ? 'Disable' : 'Enable' }}</button></td>
                    </tr>
                  } @empty {
                    <tr><td colspan="5" class="text-center text-muted py-4">No earning types configured.</td></tr>
                  }
                </tbody>
              </table>
            </div>
            <app-pagination [page]="earningPage()" [pageSize]="pageSize" [total]="earningTypes().length" (pageChange)="earningPage.set($event)"></app-pagination>
          </div>
        </section>
      </div>

      <div class="col-12 col-xl-6">
        <section class="card border-0 shadow-sm h-100">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <h2 class="h5 mb-0">Deductions</h2>
            </div>

            <form class="row g-2 mb-3" [formGroup]="deductionForm" (ngSubmit)="addDeductionType()">
              <div class="col-12 col-md-6">
                <label class="form-label">Name</label>
                <input class="form-control" formControlName="name" placeholder="e.g. Medical Aid">
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label">Code</label>
                <input class="form-control" formControlName="code" placeholder="MEDICAL">
              </div>
              <div class="col-12 col-md-3 d-flex align-items-end">
                <div class="form-check form-switch mt-3">
                  <input class="form-check-input" type="checkbox" formControlName="isEmployerContribution">
                  <label class="form-check-label">Employer</label>
                </div>
              </div>
              <div class="col-12 d-flex justify-content-end">
                <button class="btn btn-dark" type="submit" [disabled]="deductionForm.invalid">Add Deduction</button>
              </div>
            </form>

            <div class="table-responsive">
              <table class="table align-middle mb-0">
                <thead>
                  <tr><th>Name</th><th>Code</th><th>Employer</th><th>Status</th><th></th></tr>
                </thead>
                <tbody>
                  @for (item of pagedDeductionTypes(); track item.id) {
                    <tr>
                      <td>{{ item.name }}</td>
                      <td>{{ item.code }}</td>
                      <td>{{ item.isEmployerContribution ? 'Yes' : 'No' }}</td>
                      <td><span class="badge" [class.bg-success]="item.isActive" [class.bg-secondary]="!item.isActive">{{ item.isActive ? 'Active' : 'Disabled' }}</span></td>
                      <td class="text-end"><button class="btn btn-sm btn-outline-secondary" type="button" (click)="toggleDeduction(item)">{{ item.isActive ? 'Disable' : 'Enable' }}</button></td>
                    </tr>
                  } @empty {
                    <tr><td colspan="5" class="text-center text-muted py-4">No deductions configured.</td></tr>
                  }
                </tbody>
              </table>
            </div>
            <app-pagination [page]="deductionPage()" [pageSize]="pageSize" [total]="deductionTypes().length" (pageChange)="deductionPage.set($event)"></app-pagination>
          </div>
        </section>
      </div>
    </div>
  `
})
export class PayrollItemsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly settingsService = inject(SettingsService);

  readonly earningTypes = signal<EarningType[]>([]);
  readonly deductionTypes = signal<DeductionType[]>([]);
  readonly error = signal('');
  readonly pageSize = 20;
  readonly earningPage = signal(1);
  readonly deductionPage = signal(1);
  readonly pagedEarningTypes = computed(() => this.earningTypes().slice((this.earningPage() - 1) * this.pageSize, this.earningPage() * this.pageSize));
  readonly pagedDeductionTypes = computed(() => this.deductionTypes().slice((this.deductionPage() - 1) * this.pageSize, this.deductionPage() * this.pageSize));

  readonly earningForm = this.fb.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    isTaxable: [true]
  });

  readonly deductionForm = this.fb.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    isEmployerContribution: [false]
  });

  constructor() {
    this.load();
  }

  private load(): void {
    const companyId = this.auth.companyId();
    if (!companyId) return;

    this.settingsService.getEarningTypes(companyId).subscribe({
      next: values => this.earningTypes.set(values),
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load earning types.')
    });

    this.settingsService.getDeductionTypes(companyId).subscribe({
      next: values => this.deductionTypes.set(values),
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load deduction types.')
    });
  }

  addEarningType(): void {
    const companyId = this.auth.companyId();
    if (!companyId) return;

    const value = this.earningForm.getRawValue();
    this.settingsService.createEarningType({
      companyId,
      name: value.name!.trim(),
      code: value.code!.trim(),
      isTaxable: value.isTaxable ?? true
    }).subscribe({
      next: () => {
        this.earningForm.reset({ isTaxable: true });
        this.load();
      },
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to create earning type.')
    });
  }

  toggleEarning(item: EarningType): void {
    this.settingsService.updateEarningType(item.id, {
      name: item.name,
      code: item.code,
      isTaxable: item.isTaxable,
      isActive: !item.isActive
    }).subscribe({
      next: () => this.load(),
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to update earning type.')
    });
  }

  addDeductionType(): void {
    const companyId = this.auth.companyId();
    if (!companyId) return;

    const value = this.deductionForm.getRawValue();
    this.settingsService.createDeductionType({
      companyId,
      name: value.name!.trim(),
      code: value.code!.trim(),
      isEmployerContribution: value.isEmployerContribution ?? false
    }).subscribe({
      next: () => {
        this.deductionForm.reset({ isEmployerContribution: false });
        this.load();
      },
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to create deduction type.')
    });
  }

  toggleDeduction(item: DeductionType): void {
    this.settingsService.updateDeductionType(item.id, {
      name: item.name,
      code: item.code,
      isEmployerContribution: item.isEmployerContribution,
      isActive: !item.isActive
    }).subscribe({
      next: () => this.load(),
      error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to update deduction type.')
    });
  }
}
