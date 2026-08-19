import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PayrollService } from '../../services/payroll.service';
import { PayrollLine, PayrollPeriod } from '../../models/payroll.models';

@Component({
	selector: 'app-payroll-detail',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<p class="text-muted mb-1">Payroll Period</p>
				<h1 class="h3 mb-0">{{ periodName() }}</h1>
			</div>
			<a class="btn btn-outline-secondary" routerLink="/payroll">Back to Payroll Runs</a>
		</section>

		<section class="row g-3 mb-3">
			<div class="col-12 col-lg-8">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Employee Lines</h2>
						<div class="table-responsive mb-3">
							<table class="table">
								<thead><tr><th>Employee</th><th class="text-end">Gross</th><th class="text-end">Deductions</th><th class="text-end">Net Pay</th></tr></thead>
								<tbody>
									@for (line of lines(); track line.id) {
										<tr>
											<td>{{ line.employeeName }} <small class="text-muted">({{ line.employeeNumber }})</small></td>
											<td class="text-end">R {{ line.grossEarnings | number:'1.0-0' }}</td>
											<td class="text-end">R {{ line.totalDeductions | number:'1.0-0' }}</td>
											<td class="text-end">R {{ line.netPay | number:'1.0-0' }}</td>
										</tr>
									} @empty {
										<tr><td colspan="4" class="text-center text-muted py-4">No employee lines for this period.</td></tr>
									}
								</tbody>
							</table>
						</div>
					</div>
				</article>
			</div>

			<div class="col-12 col-lg-4">
				<article class="card border-0 shadow-sm mb-3">
					<div class="card-body">
						<h2 class="h6 text-muted">Summary</h2>
						<div class="d-flex justify-content-between"><span>Gross Pay</span><strong>R {{ period()?.totalGross ?? 0 | number:'1.0-0' }}</strong></div>
						<div class="d-flex justify-content-between"><span>Total Deductions</span><strong>R {{ period()?.totalDeductions ?? 0 | number:'1.0-0' }}</strong></div>
						<hr>
						<div class="d-flex justify-content-between fs-5"><span>Net Pay</span><strong>R {{ period()?.totalNet ?? 0 | number:'1.0-0' }}</strong></div>
					</div>
				</article>
				<article class="card border-0 shadow-sm">
					<div class="card-body">
						<h2 class="h6 text-muted">Actions</h2>
						<button class="btn btn-sm btn-outline-dark w-100 mb-2" type="button" (click)="approve()">Approve</button>
						<button class="btn btn-sm btn-outline-secondary w-100 mb-2" type="button" (click)="lock()">Lock</button>
						<button class="btn btn-sm btn-outline-success w-100" type="button" (click)="markPaid()">Mark Paid</button>
					</div>
				</article>
			</div>
		</section>
	`
})
export class PayrollDetailComponent {
	private readonly route = inject(ActivatedRoute);
	private readonly payrollService = inject(PayrollService);

	readonly periodId = this.route.snapshot.paramMap.get('id') ?? '';
	readonly period = signal<PayrollPeriod | null>(null);
	readonly lines = signal<PayrollLine[]>([]);

	constructor() {
		if (this.periodId) {
			this.payrollService.getById(this.periodId).subscribe(period => this.period.set(period));
			this.payrollService.getLines(this.periodId).subscribe(lines => this.lines.set(lines));
		}
	}

	periodName(): string {
		const p = this.period();
		if (!p) return '';
		return new Date(p.year, p.month - 1, 1).toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	private reload(): void {
		this.payrollService.getById(this.periodId).subscribe(period => this.period.set(period));
	}

	approve(): void {
		this.payrollService.approve(this.periodId).subscribe(() => this.reload());
	}

	lock(): void {
		this.payrollService.lock(this.periodId).subscribe(() => this.reload());
	}

	markPaid(): void {
		this.payrollService.markPaid(this.periodId).subscribe(() => this.reload());
	}
}

