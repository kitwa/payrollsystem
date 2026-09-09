import { Component, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { PayrollService } from '../../services/payroll.service';
import { PayrollPeriod, PayrollStatus } from '../../models/payroll.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
	selector: 'app-payroll-list',
	standalone: true,
	imports: [CommonModule, RouterLink, ConfirmDialogComponent],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Payroll Runs</h1>
				<p class="text-muted mb-0">Track each payroll cycle from draft to submission.</p>
			</div>
			<button class="btn btn-dark" type="button" (click)="startNewRun()"><i class="bi bi-play-circle me-2"></i>Start New Run</button>
		</section>

		@if (message()) {
			<div class="alert" [class.alert-success]="messageTone() === 'success'" [class.alert-danger]="messageTone() === 'error'">
				{{ message() }}
			</div>
		}

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
							<div class="d-flex gap-2 align-items-center flex-wrap">
								<a class="btn btn-outline-dark btn-sm" [routerLink]="['/payroll', period.id]">Open Details</a>
								<button
									class="btn btn-outline-danger btn-sm"
									type="button"
									[disabled]="period.status !== 0"
									[title]="period.status !== 0 ? 'Only Draft payroll periods can be deleted.' : 'Delete this draft payroll period.'"
									(click)="deletePeriod(period)"
								>Delete</button>
							</div>
							@if (period.status !== 0) {
								<small class="text-muted d-block mt-1">Delete is disabled — this period is {{ statusName(period.status) }} and can no longer be removed.</small>
							}
						</div>
					</article>
				</div>
			} @empty {
				<div class="col-12 text-center text-muted py-4">No payroll periods yet. Start a new run to get going.</div>
			}
		</section>

		<app-confirm-dialog #confirmDialog></app-confirm-dialog>
	`
})
export class PayrollListComponent {
	private readonly auth = inject(AuthService);
	private readonly payrollService = inject(PayrollService);

	@ViewChild('confirmDialog') confirmDialog!: ConfirmDialogComponent;

	readonly payrollPeriods = signal<PayrollPeriod[]>([]);
	readonly message = signal('');
	readonly messageTone = signal<'success' | 'error'>('success');

	constructor() {
		this.loadPeriods();
	}

	private loadPeriods(): void {
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
		this.payrollService.generate({ companyId, year: now.getFullYear(), month: now.getMonth() + 1 }).subscribe({
			next: () => { this.messageTone.set('success'); this.message.set('A new payroll run has been started.'); this.loadPeriods(); },
			error: response => { this.messageTone.set('error'); this.message.set(response.error?.errors?.[0] ?? 'Unable to start a new payroll run.'); }
		});
	}

	async deletePeriod(period: PayrollPeriod): Promise<void> {
		if (period.status !== PayrollStatus.Draft) {
			this.messageTone.set('error');
			this.message.set(`${this.periodName(period)} is ${this.statusName(period.status)} and can no longer be deleted. Only Draft periods can be removed.`);
			return;
		}

		const confirmed = await this.confirmDialog.show({
			title: 'Delete draft payroll',
			message: `Are you sure you want to delete ${this.periodName(period)}? This action cannot be undone.`,
			confirmLabel: 'Delete',
			tone: 'danger'
		});
		if (!confirmed) return;

		this.payrollService.delete(period.id).subscribe({
			next: () => { this.messageTone.set('success'); this.message.set(`${this.periodName(period)} has been deleted.`); this.loadPeriods(); },
			error: response => { this.messageTone.set('error'); this.message.set(response.error?.errors?.[0] ?? 'Unable to delete this payroll period.'); }
		});
	}
}

