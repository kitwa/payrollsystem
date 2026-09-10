import { Component, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { PayrollService } from '../../services/payroll.service';
import { PayrollPeriod, PayrollStatus } from '../../models/payroll.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { EmployeeService } from '../../../employees/services/employee.service';
import { EmployeeList } from '../../../employees/models/employee.models';
import { SettingsService } from '../../../settings/services/settings.service';
import { Company } from '../../../settings/models/settings.models';

@Component({
	selector: 'app-payroll-list',
	standalone: true,
	imports: [CommonModule, RouterLink, ConfirmDialogComponent, PaginationComponent],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Payroll Runs</h1>
				<p class="text-muted mb-0">Track each payroll cycle from draft to submission.</p>
			</div>
			<button class="btn btn-dark" type="button" (click)="startNewRun()"><i class="bi bi-play-circle me-2"></i>Start New Run</button>
		</section>

		@if (isSuperAdmin()) {
			<section class="card border-0 shadow-sm mb-3"><div class="card-body">
				<label class="form-label">Company</label>
				<select class="form-select" [value]="selectedCompanyId() ?? ''" (change)="selectCompany($event)">
					<option value="">Select company</option>
					@for (company of companies(); track company.id) { <option [value]="company.id">{{ company.name }}</option> }
				</select>
			</div></section>
		}

		@if (selectingEmployees()) {
			<section class="card border-0 shadow-sm mb-3"><div class="card-body">
				<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3"><div><h2 class="h5 mb-1">Select Employees</h2><p class="text-muted mb-0">{{ selectedEmployeeCount() }} of {{ employees().length }} employees selected</p></div><div class="d-flex gap-2"><button class="btn btn-sm btn-outline-secondary" type="button" (click)="selectAll()">Select All</button><button class="btn btn-sm btn-outline-secondary" type="button" (click)="deselectAll()">Deselect All</button></div></div>
				<input class="form-control mb-3" placeholder="Search employees" [value]="employeeSearch()" (input)="setEmployeeSearch($event)">
				<div class="row g-2 mb-3">@for (employee of filteredEmployees(); track employee.id) { <div class="col-12 col-md-6"><label class="form-check border rounded p-2"><input class="form-check-input" type="checkbox" [checked]="isSelected(employee.id)" (change)="toggleEmployee(employee.id)"><span class="form-check-label ms-2">{{ employee.firstName }} {{ employee.lastName }} <small class="text-muted">· {{ employee.employeeNumber }}</small></span></label></div> } @empty { <div class="col-12 text-muted">No active employees found.</div> }</div>
				<div class="d-flex justify-content-end gap-2"><button class="btn btn-outline-secondary" type="button" (click)="cancelEmployeeSelection()">Cancel</button><button class="btn btn-dark" type="button" [disabled]="selectedEmployeeCount() === 0 || generating()" (click)="generatePayroll()">{{ generating() ? 'Processing…' : 'Create Payroll' }}</button></div>
			</div></section>
		}

		@if (message()) {
			<div class="alert" [class.alert-success]="messageTone() === 'success'" [class.alert-danger]="messageTone() === 'error'">
				{{ message() }}
			</div>
		}

		<section class="row g-3">
			@for (period of pagedPeriods(); track period.id) {
				<div class="col-12 col-md-6 col-xl-4">
					<article class="card border-0 shadow-sm h-100">
						<div class="card-body">
							<div class="d-flex justify-content-between align-items-center mb-2">
								<h2 class="h5 mb-0">{{ periodName(period) }}</h2>
								<span class="badge" [class.bg-success]="period.status === 3" [class.bg-warning]="period.status === 1" [class.bg-info]="period.status === 2" [class.bg-secondary]="period.status === 0">
									{{ statusName(period.status) }}
								</span>
							</div>
							<p class="text-muted small mb-2">{{ period.employeeCount }} employees</p>
							<p class="fs-5 fw-bold mb-3">R {{ period.totalNet | number:'1.0-0' }}</p>
							<div class="d-flex gap-2 align-items-center flex-wrap">
								<a class="btn btn-outline-dark btn-sm" [routerLink]="['/payroll', period.id]">Open Details</a>
								<button
									class="btn btn-outline-danger btn-sm"
									type="button"
									[disabled]="!isSuperAdmin()"
									[title]="isSuperAdmin() ? 'Delete this payroll for support.' : 'Only Super Admin users can delete payroll.'"
									(click)="deletePeriod(period)"
								>Delete</button>
							</div>
							@if (!isSuperAdmin()) { <small class="text-muted d-block mt-1">Payroll deletion is restricted to Super Admin support users.</small> }
						</div>
					</article>
				</div>
			} @empty {
				<div class="col-12 text-center text-muted py-4">No payroll periods yet. Start a new run to get going.</div>
			}
		</section>
		<app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="payrollPeriods().length" (pageChange)="pageNumber.set($event)"></app-pagination>

		<app-confirm-dialog #confirmDialog></app-confirm-dialog>
	`
})
export class PayrollListComponent {
	private readonly auth = inject(AuthService);
	private readonly payrollService = inject(PayrollService);
	private readonly employeeService = inject(EmployeeService);
	private readonly settingsService = inject(SettingsService);

	@ViewChild('confirmDialog') confirmDialog!: ConfirmDialogComponent;

	readonly payrollPeriods = signal<PayrollPeriod[]>([]);
	readonly message = signal('');
	readonly messageTone = signal<'success' | 'error'>('success');
	readonly pageNumber = signal(1);
	readonly pageSize = 20;
	readonly pagedPeriods = computed(() => this.payrollPeriods().slice((this.pageNumber() - 1) * this.pageSize, this.pageNumber() * this.pageSize));
	readonly companies = signal<Company[]>([]);
	readonly selectedCompanyId = signal<string | null>(this.auth.companyId());
	readonly employees = signal<EmployeeList[]>([]);
	readonly selectingEmployees = signal(false);
	readonly employeeSearch = signal('');
	readonly selectedEmployeeIds = signal<string[]>([]);
	readonly generating = signal(false);
	readonly isSuperAdmin = computed(() => this.auth.isInRole('SuperAdmin'));
	readonly filteredEmployees = computed(() => {
		const search = this.employeeSearch().trim().toLowerCase();
		return this.employees().filter(employee => !search || `${employee.firstName} ${employee.lastName} ${employee.employeeNumber} ${employee.email ?? ''}`.toLowerCase().includes(search));
	});
	readonly selectedEmployeeCount = computed(() => this.selectedEmployeeIds().length);

	constructor() {
		if (this.isSuperAdmin()) {
			this.settingsService.getCompanies().subscribe(companies => {
				this.companies.set(companies);
				if (companies.length) this.selectedCompanyId.set(companies[0].id);
				this.loadPeriods();
			});
			return;
		}
		this.loadPeriods();
	}

	private loadPeriods(): void {
		const companyId = this.selectedCompanyId();
		if (companyId) {
			this.payrollService.getPeriods(companyId).subscribe(periods => this.payrollPeriods.set(periods));
		}
	}

	selectCompany(event: Event): void {
		this.selectedCompanyId.set((event.target as HTMLSelectElement).value || null);
		this.selectingEmployees.set(false);
		this.loadPeriods();
	}

	periodName(period: PayrollPeriod): string {
		const date = new Date(period.year, period.month - 1, 1);
		return date.toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	statusName(status: PayrollStatus): string {
		return PayrollStatus[status];
	}

	startNewRun(): void {
		const companyId = this.selectedCompanyId();
		if (!companyId) return;
		this.employeeService.getAll(companyId, 1, 100).subscribe({
			next: page => {
				this.employees.set(page.items);
				this.selectedEmployeeIds.set(page.items.map(employee => employee.id));
				this.employeeSearch.set('');
				this.selectingEmployees.set(true);
			},
			error: response => { this.messageTone.set('error'); this.message.set(response.error?.errors?.[0] ?? 'Unable to load employees.'); }
		});
	}

	generatePayroll(): void {
		const companyId = this.selectedCompanyId();
		if (!companyId || !this.selectedEmployeeCount()) return;
		const now = new Date();
		this.generating.set(true);
		this.payrollService.generate({ companyId, year: now.getFullYear(), month: now.getMonth() + 1, employeeIds: this.selectedEmployeeIds() }).subscribe({
			next: () => { this.generating.set(false); this.selectingEmployees.set(false); this.messageTone.set('success'); this.message.set('A new payroll run has been started.'); this.loadPeriods(); },
			error: response => { this.generating.set(false); this.messageTone.set('error'); this.message.set(response.error?.errors?.[0] ?? 'Unable to start a new payroll run.'); }
		});
	}

	cancelEmployeeSelection(): void { this.selectingEmployees.set(false); }
	selectAll(): void { this.selectedEmployeeIds.set(this.employees().map(employee => employee.id)); }
	deselectAll(): void { this.selectedEmployeeIds.set([]); }
	isSelected(id: string): boolean { return this.selectedEmployeeIds().includes(id); }
	toggleEmployee(id: string): void { this.selectedEmployeeIds.update(ids => ids.includes(id) ? ids.filter(selectedId => selectedId !== id) : [...ids, id]); }
	setEmployeeSearch(event: Event): void { this.employeeSearch.set((event.target as HTMLInputElement).value); }

	async deletePeriod(period: PayrollPeriod): Promise<void> {
		if (!this.isSuperAdmin()) {
			this.messageTone.set('error');
			this.message.set('Only Super Admin users can delete payroll periods.');
			return;
		}

		const confirmed = await this.confirmDialog.show({
			title: 'Delete payroll for support',
			message: `Company: ${this.companyName(period.companyId)}\nPayroll period: ${this.periodName(period)}\nEmployees: ${period.employeeCount}\n\nWarning: this payroll will be permanently deleted, including its payroll lines and payslip data.`,
			confirmLabel: 'Delete',
			tone: 'danger'
		});
		if (!confirmed) return;

		this.payrollService.delete(period.id).subscribe({
			next: () => { this.messageTone.set('success'); this.message.set(`${this.periodName(period)} has been deleted.`); this.loadPeriods(); },
			error: response => { this.messageTone.set('error'); this.message.set(response.error?.errors?.[0] ?? 'Unable to delete this payroll period.'); }
		});
	}

	companyName(companyId: string): string { return this.companies().find(company => company.id === companyId)?.name ?? 'Current company'; }
}

