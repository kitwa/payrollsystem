import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { ReportsService } from '../../services/reports.service';
import { PayrollService } from '../../../payroll/services/payroll.service';

@Component({
	selector: 'app-report-selector',
	standalone: true,
	imports: [CommonModule, ReactiveFormsModule],
	template: `
		<section class="mb-3">
			<h1 class="h3 mb-1">Reports</h1>
			<p class="text-muted mb-0">Generate finance, tax, and workforce reports with date-range filters.</p>
		</section>

		<section class="row g-3">
			<div class="col-12 col-xl-7">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Report Library</h2>
						<div class="row g-2">
							@for (report of reports; track report.name) {
								<div class="col-12 col-md-6">
									<button type="button" class="report-card" (click)="selectReport(report.name)">
										<p class="fw-semibold mb-1">{{ report.name }}</p>
										<small class="text-muted">{{ report.description }}</small>
									</button>
								</div>
							}
						</div>
					</div>
				</article>
			</div>

			<div class="col-12 col-xl-5">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Report Parameters</h2>
						<form class="row g-2" [formGroup]="form" (ngSubmit)="generate()">
							<div class="col-12">
								<label class="form-label">Report</label>
								<input type="text" class="form-control" formControlName="reportName" readonly>
							</div>
							<div class="col-12 col-md-6">
								<label class="form-label">From</label>
								<input type="date" class="form-control" formControlName="fromDate">
							</div>
							<div class="col-12 col-md-6">
								<label class="form-label">To</label>
								<input type="date" class="form-control" formControlName="toDate">
							</div>
							<div class="col-12">
								<button class="btn btn-dark w-100" type="submit" [disabled]="form.invalid">Generate Report</button>
							</div>
						</form>

						@if (result()) {
							<div class="alert alert-success mt-3 mb-0">{{ result() }}</div>
						}
					</div>
				</article>
			</div>
		</section>
	`,
	styles: [
		`
			.report-card {
				width: 100%;
				text-align: left;
				border: 1px solid #e7e5e4;
				border-radius: 0.8rem;
				padding: 0.75rem;
				background: #fff;
			}

			.report-card:hover {
				border-color: #cbd5e1;
				background: #f8fafc;
			}
		`
	]
})
export class ReportSelectorComponent {
	private readonly fb = inject(FormBuilder);
	private readonly auth = inject(AuthService);
	private readonly reportsService = inject(ReportsService);
	private readonly payrollService = inject(PayrollService);

	readonly result = signal<string | null>(null);

	readonly reports = [
		{ name: 'Payroll Summary', description: 'Gross, deductions, and net totals by period' },
		{ name: 'Department Costing', description: 'Cost center payroll analysis by department' },
		{ name: 'PAYE Liability', description: 'PAYE obligations with monthly trend lines' },
		{ name: 'Leave Liability', description: 'Accrual and projected leave payout exposure' }
	];

	readonly form = this.fb.group({
		reportName: ['Payroll Summary', Validators.required],
		fromDate: ['', Validators.required],
		toDate: ['', Validators.required]
	});

	selectReport(name: string): void {
		this.form.patchValue({ reportName: name });
		this.result.set(null);
	}

	generate(): void {
		if (this.form.invalid) {
			return;
		}
		const companyId = this.auth.companyId();
		if (!companyId) return;

		const { reportName, fromDate, toDate } = this.form.getRawValue();

		if (reportName === 'Leave Liability') {
			this.reportsService.leaveReport(companyId, fromDate!, toDate!).subscribe(rows => {
				this.result.set(`Leave Liability generated: ${rows.length} leave record(s) found.`);
			});
			return;
		}

		this.payrollService.getPeriods(companyId).subscribe(periods => {
			const latest = periods[0];
			if (!latest) {
				this.result.set('No payroll period available to report on yet.');
				return;
			}

			if (reportName === 'Payroll Summary') {
				this.reportsService.payrollRegister(latest.id).subscribe(reg => {
					this.result.set(`Payroll Summary generated: Gross R${reg.totalGross.toLocaleString()}, Net R${reg.totalNet.toLocaleString()}.`);
				});
			} else if (reportName === 'PAYE Liability') {
				this.reportsService.taxReport(latest.id).subscribe(rep => {
					this.result.set(`PAYE Liability generated: R${rep.totalEmployee.toLocaleString()} across ${rep.lines.length} employee(s).`);
				});
			} else if (reportName === 'Department Costing') {
				this.reportsService.employeeCost(companyId, latest.year).subscribe(rep => {
					this.result.set(`Department Costing generated: Total cost R${rep.grandTotal.toLocaleString()} for ${latest.year}.`);
				});
			}
		});
	}
}

