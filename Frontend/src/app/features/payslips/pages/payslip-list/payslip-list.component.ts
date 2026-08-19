import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/auth/auth.service';
import { PayslipService } from '../../services/payslip.service';
import { Payslip } from '../../models/payslip.models';

@Component({
	selector: 'app-payslip-list',
	standalone: true,
	imports: [CommonModule],
	template: `
		<section class="mb-3">
			<h1 class="h3 mb-1">Payslips</h1>
			<p class="text-muted mb-0">Access your generated payslips per payroll cycle.</p>
		</section>

		<section class="row g-3">
			@for (payslip of payslips(); track payslip.payrollLineId) {
				<div class="col-12 col-md-6 col-xl-4">
					<article class="card border-0 shadow-sm h-100">
						<div class="card-body">
							<div class="d-flex justify-content-between align-items-center mb-2">
								<h2 class="h5 mb-0">{{ periodName(payslip) }}</h2>
								<span class="badge bg-success">Net R {{ payslip.netPay | number:'1.0-0' }}</span>
							</div>
							<p class="text-muted mb-1">Gross: R {{ payslip.grossEarnings | number:'1.0-0' }}</p>
							<p class="text-muted mb-3">Deductions: R {{ payslip.totalDeductions | number:'1.0-0' }}</p>
							<div class="d-flex gap-2 flex-wrap">
								<button type="button" class="btn btn-sm btn-outline-dark" (click)="download(payslip)">Download PDF</button>
							</div>
						</div>
					</article>
				</div>
			} @empty {
				<div class="col-12 text-center text-muted py-4">No payslips available yet.</div>
			}
		</section>
	`
})
export class PayslipListComponent {
	private readonly auth = inject(AuthService);
	private readonly payslipService = inject(PayslipService);

	readonly payslips = signal<Payslip[]>([]);

	constructor() {
		const employeeId = this.auth.employeeId();
		if (employeeId) {
			this.payslipService.getForEmployee(employeeId).subscribe(payslips => this.payslips.set(payslips));
		}
	}

	periodName(payslip: Payslip): string {
		return new Date(payslip.year, payslip.month - 1, 1).toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });
	}

	download(payslip: Payslip): void {
		this.payslipService.download(payslip.payrollLineId).subscribe(blob => {
			const url = window.URL.createObjectURL(blob);
			const link = document.createElement('a');
			link.href = url;
			link.download = `payslip-${payslip.year}-${payslip.month}.pdf`;
			link.click();
			window.URL.revokeObjectURL(url);
		});
	}
}

