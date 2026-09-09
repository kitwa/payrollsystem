import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PayslipService } from '../../services/payslip.service';
import { PayslipDetail } from '../../models/payslip.models';

@Component({
	selector: 'app-payslip-detail',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<p class="text-muted mb-1">Payslip</p>
				<h1 class="h3 mb-0">{{ periodName() }}</h1>
			</div>
			<div class="d-flex gap-2 flex-wrap">
				<a class="btn btn-outline-secondary" routerLink="/payslips">Back to Payslips</a>
				<button class="btn btn-dark" type="button" [disabled]="!payslip()" (click)="download()">Download PDF</button>
			</div>
		</section>

		@if (error()) {
			<div class="alert alert-danger">{{ error() }}</div>
		}

		@if (payslip(); as slip) {
			<section class="card border-0 shadow-sm mb-3">
				<div class="card-body">
					<div class="row g-3">
						<div class="col-12 col-md-6">
							<h2 class="h5 mb-2">{{ slip.companyName }}</h2>
							<p class="mb-0 fw-semibold">{{ slip.employeeName }}</p>
							<small class="text-muted d-block">{{ slip.employeeNumber }}</small>
							<small class="text-muted d-block">{{ slip.jobTitle }}{{ slip.department ? ' · ' + slip.department : '' }}</small>
						</div>
						<div class="col-12 col-md-6 text-md-end">
							<p class="mb-1"><span class="text-muted">Pay period:</span> {{ slip.periodStart | date:'yyyy-MM-dd' }} to {{ slip.periodEnd | date:'yyyy-MM-dd' }}</p>
							<p class="mb-1"><span class="text-muted">Tax number:</span> {{ slip.taxNumber || '—' }}</p>
							<p class="mb-1"><span class="text-muted">UIF number:</span> {{ slip.uifNumber || '—' }}</p>
							<p class="mb-0">
								<span class="text-muted">Emailed:</span>
								@if (slip.emailedAt) {
									<span class="badge bg-success">{{ slip.emailedAt | date:'yyyy-MM-dd HH:mm' }}</span>
								} @else {
									<span class="badge bg-secondary">Not sent</span>
								}
							</p>
						</div>
					</div>
				</div>
			</section>

			<section class="row g-3">
				<div class="col-12 col-lg-6">
					<article class="card border-0 shadow-sm h-100">
						<div class="card-body">
							<h2 class="h5 mb-3">Earnings</h2>
							<table class="table mb-0">
								<thead><tr><th>Description</th><th class="text-end">Amount</th></tr></thead>
								<tbody>
									@for (item of slip.earnings; track item.description) {
										<tr><td>{{ item.description }}</td><td class="text-end">R {{ item.amount | number:'1.2-2' }}</td></tr>
									} @empty {
										<tr><td colspan="2" class="text-center text-muted py-3">No earnings captured.</td></tr>
									}
								</tbody>
								<tfoot>
									<tr class="fw-semibold"><td>Gross Earnings</td><td class="text-end">R {{ slip.grossEarnings | number:'1.2-2' }}</td></tr>
								</tfoot>
							</table>
						</div>
					</article>
				</div>

				<div class="col-12 col-lg-6">
					<article class="card border-0 shadow-sm h-100">
						<div class="card-body">
							<h2 class="h5 mb-3">Deductions</h2>
							<table class="table mb-0">
								<thead><tr><th>Description</th><th class="text-end">Amount</th></tr></thead>
								<tbody>
									@for (item of slip.deductions; track item.description) {
										<tr><td>{{ item.description }}</td><td class="text-end">R {{ item.amount | number:'1.2-2' }}</td></tr>
									} @empty {
										<tr><td colspan="2" class="text-center text-muted py-3">No deductions captured.</td></tr>
									}
								</tbody>
								<tfoot>
									<tr class="fw-semibold"><td>Total Deductions</td><td class="text-end">R {{ slip.totalDeductions | number:'1.2-2' }}</td></tr>
								</tfoot>
							</table>
						</div>
					</article>
				</div>

				<div class="col-12">
					<article class="card border-0 shadow-sm">
						<div class="card-body d-flex justify-content-between align-items-center">
							<h2 class="h5 mb-0">Net Pay</h2>
							<strong class="fs-4">R {{ slip.netPay | number:'1.2-2' }}</strong>
						</div>
					</article>
				</div>
			</section>
		}
	`
})
export class PayslipDetailComponent {
	private readonly route = inject(ActivatedRoute);
	private readonly payslipService = inject(PayslipService);

	readonly payslipId = this.route.snapshot.paramMap.get('id') ?? '';
	readonly payslip = signal<PayslipDetail | null>(null);
	readonly error = signal('');

	constructor() {
		if (this.payslipId) {
			this.payslipService.getDetail(this.payslipId).subscribe({
				next: detail => this.payslip.set(detail),
				error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load this payslip.')
			});
		}
	}

	periodName(): string {
		const slip = this.payslip();
		if (!slip) return 'Payslip';
		return new Date(slip.year, slip.month - 1, 1).toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	download(): void {
		const slip = this.payslip();
		if (!slip) return;
		this.payslipService.download(slip.payrollLineId).subscribe({
			next: blob => this.payslipService.saveBlob(blob, `payslip-${slip.employeeNumber}-${slip.year}-${slip.month}.pdf`),
			error: () => this.error.set('Unable to download this payslip.')
		});
	}
}
