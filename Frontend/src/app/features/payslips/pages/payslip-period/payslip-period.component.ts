import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PayslipService } from '../../services/payslip.service';
import { Payslip } from '../../models/payslip.models';

@Component({
	selector: 'app-payslip-period',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<p class="text-muted mb-1">Payslip Batch</p>
				<h1 class="h3 mb-0">{{ periodName() }}</h1>
			</div>
			<div class="d-flex gap-2 flex-wrap">
				<a class="btn btn-outline-secondary" routerLink="/payslips">Back to Payslips</a>
				<button class="btn btn-outline-dark" type="button" [disabled]="!payslips().length || busy()" (click)="downloadZip()">Download ZIP</button>
				<button class="btn btn-dark" type="button" [disabled]="!payslips().length || busy()" (click)="emailAll()">Email All</button>
			</div>
		</section>

		@if (error()) {
			<div class="alert alert-danger">{{ error() }}</div>
		}
		@if (message()) {
			<div class="alert alert-success">{{ message() }}</div>
		}

		<section class="row g-3 mb-3">
			<div class="col-6 col-lg-3">
				<article class="card border-0 shadow-sm"><div class="card-body">
					<p class="text-muted mb-1 small">Payslips</p><strong class="fs-5">{{ payslips().length }}</strong>
				</div></article>
			</div>
			<div class="col-6 col-lg-3">
				<article class="card border-0 shadow-sm"><div class="card-body">
					<p class="text-muted mb-1 small">Emailed</p><strong class="fs-5">{{ emailedCount() }}</strong>
				</div></article>
			</div>
			<div class="col-6 col-lg-3">
				<article class="card border-0 shadow-sm"><div class="card-body">
					<p class="text-muted mb-1 small">Total Gross</p><strong class="fs-5">R {{ totalGross() | number:'1.0-0' }}</strong>
				</div></article>
			</div>
			<div class="col-6 col-lg-3">
				<article class="card border-0 shadow-sm"><div class="card-body">
					<p class="text-muted mb-1 small">Total Net</p><strong class="fs-5">R {{ totalNet() | number:'1.0-0' }}</strong>
				</div></article>
			</div>
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr>
								<th>Employee</th>
								<th class="text-end">Gross</th>
								<th class="text-end">Deductions</th>
								<th class="text-end">Net Pay</th>
								<th>Emailed</th>
								<th class="text-end">Actions</th>
							</tr>
						</thead>
						<tbody>
							@for (payslip of payslips(); track payslip.payrollLineId) {
								<tr>
									<td>
										<p class="mb-0 fw-semibold">{{ payslip.employeeName }}</p>
										<small class="text-muted">{{ payslip.employeeNumber }} · {{ payslip.email || 'No email' }}</small>
									</td>
									<td class="text-end">R {{ payslip.grossEarnings | number:'1.0-0' }}</td>
									<td class="text-end">R {{ payslip.totalDeductions | number:'1.0-0' }}</td>
									<td class="text-end">R {{ payslip.netPay | number:'1.0-0' }}</td>
									<td>
										@if (payslip.emailedAt) {
											<span class="badge bg-success">{{ payslip.emailedAt | date:'yyyy-MM-dd' }}</span>
										} @else {
											<span class="badge bg-secondary">Not sent</span>
										}
									</td>
									<td class="text-end">
										<a class="btn btn-sm btn-outline-secondary me-2" [routerLink]="['/payslips', payslip.payrollLineId]">Preview</a>
										<button class="btn btn-sm btn-outline-dark me-2" type="button" (click)="download(payslip)">PDF</button>
										<button class="btn btn-sm btn-outline-primary" type="button" [disabled]="!payslip.email || busy()" (click)="email(payslip)">Email</button>
									</td>
								</tr>
							} @empty {
								<tr><td colspan="6" class="text-center text-muted py-4">No payslips in this period.</td></tr>
							}
						</tbody>
					</table>
				</div>
			</div>
		</section>
	`
})
export class PayslipPeriodComponent {
	private readonly route = inject(ActivatedRoute);
	private readonly payslipService = inject(PayslipService);

	readonly periodId = this.route.snapshot.paramMap.get('periodId') ?? '';
	readonly payslips = signal<Payslip[]>([]);
	readonly error = signal('');
	readonly message = signal('');
	readonly busy = signal(false);

	readonly emailedCount = computed(() => this.payslips().filter(p => !!p.emailedAt).length);
	readonly totalGross = computed(() => this.payslips().reduce((sum, p) => sum + p.grossEarnings, 0));
	readonly totalNet = computed(() => this.payslips().reduce((sum, p) => sum + p.netPay, 0));

	constructor() {
		if (this.periodId) this.load();
	}

	private load(): void {
		this.payslipService.getForPeriod(this.periodId).subscribe({
			next: payslips => this.payslips.set(payslips),
			error: response => this.error.set(this.messageFrom(response, 'Unable to load payslips.'))
		});
	}

	periodName(): string {
		const first = this.payslips()[0];
		if (!first) return 'Payroll Period';
		return new Date(first.year, first.month - 1, 1).toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	download(payslip: Payslip): void {
		this.payslipService.download(payslip.payrollLineId).subscribe({
			next: blob => this.payslipService.saveBlob(blob, `payslip-${payslip.employeeNumber}-${payslip.year}-${payslip.month}.pdf`),
			error: () => this.error.set('Unable to download this payslip.')
		});
	}

	downloadZip(): void {
		this.reset();
		this.busy.set(true);
		this.payslipService.downloadPeriod(this.periodId).subscribe({
			next: blob => {
				this.payslipService.saveBlob(blob, `payslips-${this.periodId}.zip`);
				this.busy.set(false);
			},
			error: () => { this.error.set('Unable to download this payslip batch.'); this.busy.set(false); }
		});
	}

	email(payslip: Payslip): void {
		this.reset();
		this.busy.set(true);
		this.payslipService.email(payslip.payrollLineId).subscribe({
			next: () => { this.message.set(`Payslip emailed to ${payslip.employeeName}.`); this.busy.set(false); this.load(); },
			error: response => { this.error.set(this.messageFrom(response, 'Unable to email this payslip.')); this.busy.set(false); }
		});
	}

	emailAll(): void {
		this.reset();
		this.busy.set(true);
		this.payslipService.emailPeriod(this.periodId).subscribe({
			next: result => {
				this.message.set(`Sent ${result.sent} payslip(s). Skipped ${result.skipped}.`);
				if (result.failures.length) this.error.set(result.failures.join(' '));
				this.busy.set(false);
				this.load();
			},
			error: response => { this.error.set(this.messageFrom(response, 'Unable to email these payslips.')); this.busy.set(false); }
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
