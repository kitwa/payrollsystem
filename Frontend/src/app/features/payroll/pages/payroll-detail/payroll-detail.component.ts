import { Component, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PayrollService } from '../../services/payroll.service';
import { PayrollLine, PayrollPeriod, PayrollStatus } from '../../models/payroll.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
	selector: 'app-payroll-detail',
	standalone: true,
	imports: [CommonModule, RouterLink, ConfirmDialogComponent],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<p class="text-muted mb-1">Payroll Period</p>
				<h1 class="h3 mb-0">{{ periodName() }}</h1>
			</div>
			<a class="btn btn-outline-secondary" routerLink="/payroll">Back to Payroll Runs</a>
		</section>

		@if (message()) {
			<div class="alert" [class.alert-success]="messageTone() === 'success'" [class.alert-danger]="messageTone() === 'error'">
				{{ message() }}
			</div>
		}

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

						<div class="mb-3">
							<button class="btn btn-sm btn-outline-dark w-100 mb-1" type="button" [disabled]="!canApprove()" (click)="approve()">Approve</button>
							<small class="text-muted d-block">Confirms the payroll figures are correct and ready to be locked. Only available while the period is in Draft.</small>
						</div>

						<div class="mb-3">
							<button class="btn btn-sm btn-outline-secondary w-100 mb-1" type="button" [disabled]="!canLock()" (click)="lock()">Lock</button>
							<small class="text-muted d-block">Prevents any further changes to the payroll. Only available once the period has been Approved.</small>
						</div>

						<div class="mb-3">
							<button class="btn btn-sm btn-outline-success w-100 mb-1" type="button" [disabled]="!canMarkPaid()" (click)="markPaid()">Mark Paid</button>
							<small class="text-muted d-block">Confirms employees have been paid for this period. Only available once the period has been Locked.</small>
						</div>

						<div>
							<button class="btn btn-sm btn-outline-danger w-100 mb-1" type="button" [disabled]="!canDelete()" (click)="deletePeriod()">Delete Draft</button>
							@if (canDelete()) {
								<small class="text-muted d-block">Permanently removes this draft payroll period.</small>
							} @else {
								<small class="text-danger d-block">This payroll period is {{ statusName() }} and can no longer be deleted. Only Draft periods can be removed.</small>
							}
						</div>
					</div>
				</article>
			</div>
		</section>

		<app-confirm-dialog #confirmDialog></app-confirm-dialog>
	`
})
export class PayrollDetailComponent {
	private readonly route = inject(ActivatedRoute);
	private readonly router = inject(Router);
	private readonly payrollService = inject(PayrollService);

	@ViewChild('confirmDialog') confirmDialog!: ConfirmDialogComponent;

	readonly periodId = this.route.snapshot.paramMap.get('id') ?? '';
	readonly period = signal<PayrollPeriod | null>(null);
	readonly lines = signal<PayrollLine[]>([]);
	readonly message = signal('');
	readonly messageTone = signal<'success' | 'error'>('success');

	readonly canApprove = computed(() => this.period()?.status === PayrollStatus.Draft);
	readonly canLock = computed(() => this.period()?.status === PayrollStatus.Approved);
	readonly canMarkPaid = computed(() => this.period()?.status === PayrollStatus.Locked);
	readonly canDelete = computed(() => this.period()?.status === PayrollStatus.Draft);

	statusName(): string {
		const status = this.period()?.status;
		return status === undefined ? '' : PayrollStatus[status];
	}

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

	private showSuccess(text: string): void {
		this.messageTone.set('success');
		this.message.set(text);
	}

	private showError(response: { error?: { errors?: string[] } }, fallback: string): void {
		this.messageTone.set('error');
		this.message.set(response.error?.errors?.[0] ?? fallback);
	}

	async approve(): Promise<void> {
		const confirmed = await this.confirmDialog.show({
			title: 'Approve payroll period',
			message: `Are you sure you want to approve ${this.periodName()}? This confirms the payroll is ready for locking.`,
			confirmLabel: 'Approve'
		});
		if (!confirmed) return;

		this.payrollService.approve(this.periodId).subscribe({
			next: () => { this.showSuccess('Payroll period has been approved.'); this.reload(); },
			error: response => this.showError(response, 'Unable to approve this payroll period.')
		});
	}

	async lock(): Promise<void> {
		const confirmed = await this.confirmDialog.show({
			title: 'Lock payroll period',
			message: `Are you sure you want to lock ${this.periodName()}? No further changes will be allowed once locked.`,
			confirmLabel: 'Lock'
		});
		if (!confirmed) return;

		this.payrollService.lock(this.periodId).subscribe({
			next: () => { this.showSuccess('Payroll period has been locked.'); this.reload(); },
			error: response => this.showError(response, 'Unable to lock this payroll period.')
		});
	}

	async markPaid(): Promise<void> {
		const confirmed = await this.confirmDialog.show({
			title: 'Mark payroll as paid',
			message: `Are you sure you want to mark ${this.periodName()} as paid? This confirms payments have been made to employees.`,
			confirmLabel: 'Mark Paid'
		});
		if (!confirmed) return;

		this.payrollService.markPaid(this.periodId).subscribe({
			next: () => { this.showSuccess('Payroll period has been marked as paid.'); this.reload(); },
			error: response => this.showError(response, 'Unable to mark this payroll period as paid.')
		});
	}

	async deletePeriod(): Promise<void> {
		const confirmed = await this.confirmDialog.show({
			title: 'Delete draft payroll',
			message: `Are you sure you want to delete ${this.periodName()}? This action cannot be undone.`,
			confirmLabel: 'Delete',
			tone: 'danger'
		});
		if (!confirmed) return;

		this.payrollService.delete(this.periodId).subscribe({
			next: () => this.router.navigate(['/payroll']),
			error: response => this.showError(response, 'Unable to delete this payroll period.')
		});
	}
}

