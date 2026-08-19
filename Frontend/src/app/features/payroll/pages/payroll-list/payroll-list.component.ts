import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { PayrollService } from '../../services/payroll.service';
import { PayrollPeriod, PayrollStatus } from '../../models/payroll.models';

@Component({
	selector: 'app-payroll-list',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Payroll Runs</h1>
				<p class="text-muted mb-0">Track each payroll cycle from draft to submission.</p>
			</div>
			<button class="btn btn-dark" type="button" (click)="startNewRun()"><i class="bi bi-play-circle me-2"></i>Start New Run</button>
		</section>

		<section class="row g-3">
			@for (period of payrollPeriods(); track period.id) {
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
							<a class="btn btn-outline-dark btn-sm" [routerLink]="['/payroll', period.id]">Open Details</a>
						</div>
					</article>
				</div>
			} @empty {
				<div class="col-12 text-center text-muted py-4">No payroll periods yet. Start a new run to get going.</div>
			}
		</section>
	`
})
export class PayrollListComponent {
	private readonly auth = inject(AuthService);
	private readonly payrollService = inject(PayrollService);

	readonly payrollPeriods = signal<PayrollPeriod[]>([]);

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.payrollService.getPeriods(companyId).subscribe(periods => this.payrollPeriods.set(periods));
		}
	}

	periodName(period: PayrollPeriod): string {
		const date = new Date(period.year, period.month - 1, 1);
		return date.toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	statusName(status: PayrollStatus): string {
		return PayrollStatus[status];
	}

	startNewRun(): void {
		const companyId = this.auth.companyId();
		if (!companyId) return;
		const now = new Date();
		this.payrollService.generate({ companyId, year: now.getFullYear(), month: now.getMonth() + 1 }).subscribe(() => {
			this.payrollService.getPeriods(companyId).subscribe(periods => this.payrollPeriods.set(periods));
		});
	}
}

