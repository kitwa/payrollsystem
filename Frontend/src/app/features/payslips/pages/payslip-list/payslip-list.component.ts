import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { PayslipService } from '../../services/payslip.service';
import { Payslip, PayslipPeriodSummary } from '../../models/payslip.models';
import { SettingsService } from '../../../settings/services/settings.service';
import { Company } from '../../../settings/models/settings.models';

@Component({
	selector: 'app-payslip-list',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Payslips</h1>
				<p class="text-muted mb-0">
					{{ isManager() ? 'Distribute and download payslips per payroll cycle.' : 'Access your generated payslips per payroll cycle.' }}
				</p>
			</div>
		</section>

		@if (error()) {
			<div class="alert alert-danger">{{ error() }}</div>
		}
		@if (message()) {
			<div class="alert alert-success">{{ message() }}</div>
		}

		@if (isManager()) {
			@if (canSelectCompany()) {
				<div class="row mb-3">
					<div class="col-12 col-md-5">
						<label class="form-label">Company</label>
						<select class="form-select" [value]="selectedCompanyId() ?? ''" (change)="selectCompany($event)">
							<option value="">Select company</option>
							@for (company of companies(); track company.id) {
								<option [value]="company.id">{{ company.name }}</option>
							}
						</select>
					</div>
				</div>
			}
			<section class="row g-3">
				@for (period of periods(); track period.payrollPeriodId) {
					<div class="col-12 col-md-6 col-xl-4">
						<article class="card border-0 shadow-sm h-100">
							<div class="card-body">
								<div class="d-flex justify-content-between align-items-center mb-2">
									<h2 class="h5 mb-0">{{ periodName(period.year, period.month) }}</h2>
									<span class="badge" [class.bg-success]="period.emailedCount === period.payslipCount && period.payslipCount > 0" [class.bg-secondary]="period.emailedCount !== period.payslipCount || period.payslipCount === 0">
										{{ period.emailedCount }}/{{ period.payslipCount }} sent
									</span>
								</div>
								<p class="text-muted mb-1">Employees: {{ period.payslipCount }}</p>
								<p class="text-muted mb-3">Total net: R {{ period.totalNet | number:'1.0-0' }}</p>
								<div class="d-flex gap-2 flex-wrap">
									<a class="btn btn-sm btn-outline-dark" [routerLink]="['/payslips/period', period.payrollPeriodId]">Open</a>
									<button type="button" class="btn btn-sm btn-outline-secondary" [disabled]="!period.payslipCount || busy()" (click)="downloadZip(period)">Download ZIP</button>
									<button type="button" class="btn btn-sm btn-outline-primary" [disabled]="!period.payslipCount || busy()" (click)="emailAll(period)">Email All</button>
								</div>
							</div>
						</article>
					</div>
				} @empty {
					<div class="col-12 text-center text-muted py-4">No payroll periods with payslips yet.</div>
				}
			</section>
		} @else {
			<section class="row g-3">
				@for (payslip of payslips(); track payslip.payrollLineId) {
					<div class="col-12 col-md-6 col-xl-4">
						<article class="card border-0 shadow-sm h-100">
							<div class="card-body">
								<div class="d-flex justify-content-between align-items-center mb-2">
									<h2 class="h5 mb-0">{{ periodName(payslip.year, payslip.month) }}</h2>
									<span class="badge bg-success">Net R {{ payslip.netPay | number:'1.0-0' }}</span>
								</div>
								<p class="text-muted mb-1">Gross: R {{ payslip.grossEarnings | number:'1.0-0' }}</p>
								<p class="text-muted mb-3">Deductions: R {{ payslip.totalDeductions | number:'1.0-0' }}</p>
								<div class="d-flex gap-2 flex-wrap">
									<a class="btn btn-sm btn-outline-secondary" [routerLink]="['/payslips', payslip.payrollLineId]">Preview</a>
									<button type="button" class="btn btn-sm btn-outline-dark" (click)="download(payslip)">Download PDF</button>
								</div>
							</div>
						</article>
					</div>
				} @empty {
					<div class="col-12 text-center text-muted py-4">No payslips available yet.</div>
				}
			</section>
		}
	`
})
export class PayslipListComponent {
	private readonly auth = inject(AuthService);
	private readonly payslipService = inject(PayslipService);
	private readonly settingsService = inject(SettingsService);

	readonly payslips = signal<Payslip[]>([]);
	readonly periods = signal<PayslipPeriodSummary[]>([]);
	readonly companies = signal<Company[]>([]);
	readonly selectedCompanyId = signal<string | null>(this.auth.companyId());
	readonly error = signal('');
	readonly message = signal('');
	readonly busy = signal(false);

	readonly isManager = computed(() =>
		['PayrollManager', 'Admin', 'SuperAdmin'].some(role => this.auth.isInRole(role)));
	readonly canSelectCompany = computed(() => !this.auth.companyId() && this.auth.isInRole('SuperAdmin'));

	constructor() {
		if (this.isManager()) {
			if (this.canSelectCompany()) {
				this.settingsService.getCompanies().subscribe({
					next: companies => {
						this.companies.set(companies);
						if (companies.length) {
							this.selectedCompanyId.set(companies[0].id);
							this.loadPeriods();
						}
					},
					error: response => this.error.set(this.messageFrom(response, 'Unable to load companies.'))
				});
			} else {
				this.loadPeriods();
			}
		} else {
			this.payslipService.getMine().subscribe({
				next: payslips => this.payslips.set(payslips),
				error: response => this.error.set(this.messageFrom(response, 'Unable to load payslips.'))
			});
		}
	}

	private loadPeriods(): void {
		const companyId = this.selectedCompanyId();
		if (!companyId) {
			this.error.set('Your account is not linked to a company.');
			return;
		}

		this.payslipService.getPeriods(companyId).subscribe({
			next: periods => this.periods.set(periods),
			error: response => this.error.set(this.messageFrom(response, 'Unable to load payslip batches.'))
		});
	}

	selectCompany(event: Event): void {
		const companyId = (event.target as HTMLSelectElement).value || null;
		this.selectedCompanyId.set(companyId);
		this.periods.set([]);
		this.reset();
		if (companyId) this.loadPeriods();
	}

	periodName(year: number, month: number): string {
		return new Date(year, month - 1, 1).toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	download(payslip: Payslip): void {
		this.payslipService.download(payslip.payrollLineId).subscribe({
			next: blob => this.payslipService.saveBlob(blob, `payslip-${payslip.year}-${payslip.month}.pdf`),
			error: () => this.error.set('Unable to download this payslip.')
		});
	}

	downloadZip(period: PayslipPeriodSummary): void {
		this.reset();
		this.busy.set(true);
		this.payslipService.downloadPeriod(period.payrollPeriodId).subscribe({
			next: blob => {
				this.payslipService.saveBlob(blob, `payslips-${period.year}-${period.month}.zip`);
				this.busy.set(false);
			},
			error: () => { this.error.set('Unable to download this payslip batch.'); this.busy.set(false); }
		});
	}

	emailAll(period: PayslipPeriodSummary): void {
		this.reset();
		this.busy.set(true);
		this.payslipService.emailPeriod(period.payrollPeriodId).subscribe({
			next: result => {
				this.message.set(`Sent ${result.sent} payslip(s). Skipped ${result.skipped}.`);
				if (result.failures.length) this.error.set(result.failures.join(' '));
				this.busy.set(false);
				this.loadPeriods();
			},
			error: response => {
				this.error.set(this.messageFrom(response, 'Unable to email this payslip batch.'));
				this.busy.set(false);
			}
		});
	}

	private reset(): void {
		this.error.set('');
		this.message.set('');
	}

	private messageFrom(response: { error?: { errors?: string[] } }, fallback: string): string {
		return response.error?.errors?.[0] ?? fallback;
	}
}

