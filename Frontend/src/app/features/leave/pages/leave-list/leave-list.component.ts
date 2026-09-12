import { Component, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { LeaveService } from '../../services/leave.service';
import { LeaveRequest, LeaveStatus } from '../../models/leave.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
	selector: 'app-leave-list',
	standalone: true,
	imports: [CommonModule, RouterLink, ConfirmDialogComponent, PaginationComponent],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Leave Requests</h1>
				<p class="text-muted mb-0">Review employee leave, balances, and manager approvals.</p>
			</div>
			<a class="btn btn-dark" routerLink="/leave/request">New Leave Request</a>
			@if (isManager()) {
				<a class="btn btn-outline-dark" routerLink="/leave/balances">Leave Balances</a>
			}
		</section>

		@if (message()) {
			<div class="alert" [class.alert-success]="messageTone() === 'success'" [class.alert-danger]="messageTone() === 'error'">
				{{ message() }}
			</div>
		}

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="row g-2 mb-3">
					<div class="col-12 col-md-4">
						<label class="form-label">Filter by status</label>
						<select class="form-select" [value]="selectedStatus()" (change)="setStatus($event)">
							@for (status of statuses; track status) {
								<option [value]="status">{{ status }}</option>
							}
						</select>
					</div>
				</div>

				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr>
								<th>Employee</th>
								<th>Type</th>
								<th>Dates</th>
								<th>Days</th>
								<th>Status</th>
								@if (isManager()) { <th class="text-end">Actions</th> }
							</tr>
						</thead>
						<tbody>
							@for (leave of pagedLeaves(); track leave.id) {
								<tr>
									<td>{{ leave.employeeName }}</td>
									<td>{{ leave.leaveTypeName }}</td>
									<td>{{ leave.startDate | date:'yyyy-MM-dd' }} to {{ leave.endDate | date:'yyyy-MM-dd' }}</td>
									<td>{{ leave.days }}</td>
									<td><span class="badge" [class.bg-success]="leave.status === 1" [class.bg-warning]="leave.status === 0" [class.bg-danger]="leave.status === 2">{{ statusName(leave.status) }}</span></td>
									@if (isManager()) {
										<td class="text-end">
											@if (leave.status === 0) {
												<button class="btn btn-sm btn-outline-success me-2" type="button" (click)="approve(leave)">Approve</button>
												<button class="btn btn-sm btn-outline-danger" type="button" (click)="reject(leave)">Reject</button>
											} @else {
												<small class="text-muted">{{ leave.status === 1 ? 'Approved' : 'Reviewed' }}{{ leave.reviewedBy ? ' by ' + leave.reviewedBy : '' }}</small>
											}
										</td>
									}
								</tr>
							} @empty {
								<tr><td [attr.colspan]="isManager() ? 6 : 5" class="text-center text-muted py-4">No leave requests found.</td></tr>
							}
						</tbody>
					</table>
				</div>
				<app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="filteredLeaves().length" (pageChange)="pageNumber.set($event)"></app-pagination>
			</div>
		</section>

		<app-confirm-dialog #confirmDialog></app-confirm-dialog>
	`
})
export class LeaveListComponent {
	private readonly auth = inject(AuthService);
	private readonly leaveService = inject(LeaveService);

	@ViewChild('confirmDialog') confirmDialog!: ConfirmDialogComponent;

	readonly statuses = ['All', 'Pending', 'Approved', 'Rejected'];
	readonly selectedStatus = signal('All');
	readonly leaveRequests = signal<LeaveRequest[]>([]);
	readonly message = signal('');
	readonly messageTone = signal<'success' | 'error'>('success');
	readonly pageNumber = signal(1);
	readonly pageSize = 20;

	readonly isManager = computed(() =>
		['PayrollManager', 'Admin', 'SuperAdmin'].some(role => this.auth.isInRole(role)));

	constructor() {
		this.loadLeaves();
	}

	private loadLeaves(): void {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.leaveService.getAll(companyId).subscribe(requests => this.leaveRequests.set(requests));
		}
	}

	readonly filteredLeaves = computed(() => {
		const status = this.selectedStatus();
		if (status === 'All') return this.leaveRequests();
		const statusIndex = this.statuses.indexOf(status) - 1;
		return this.leaveRequests().filter(item => item.status === statusIndex);
	});

	readonly pagedLeaves = computed(() => {
		const start = (this.pageNumber() - 1) * this.pageSize;
		return this.filteredLeaves().slice(start, start + this.pageSize);
	});

	statusName(status: LeaveStatus): string {
		return LeaveStatus[status];
	}

	setStatus(event: Event): void {
		this.selectedStatus.set((event.target as HTMLSelectElement).value);
		this.pageNumber.set(1);
	}

	async approve(leave: LeaveRequest): Promise<void> {
		const confirmed = await this.confirmDialog.show({
			title: 'Approve leave request',
			message: `Approve ${leave.leaveTypeName} for ${leave.employeeName} (${leave.days} day(s))?`,
			confirmLabel: 'Approve'
		});
		if (!confirmed) return;

		this.leaveService.approve(leave.id).subscribe({
			next: () => { this.showSuccess(`Leave request for ${leave.employeeName} has been approved.`); this.loadLeaves(); },
			error: response => this.showError(response, 'Unable to approve this leave request.')
		});
	}

	async reject(leave: LeaveRequest): Promise<void> {
		const note = window.prompt(`Reason for rejecting ${leave.employeeName}'s leave request:`);
		if (note === null) return;
		if (!note.trim()) {
			this.showError({}, 'A reason is required to reject a leave request.');
			return;
		}

		const confirmed = await this.confirmDialog.show({
			title: 'Reject leave request',
			message: `Reject ${leave.leaveTypeName} for ${leave.employeeName}? They will see: "${note}"`,
			confirmLabel: 'Reject',
			tone: 'danger'
		});
		if (!confirmed) return;

		this.leaveService.reject(leave.id, note.trim()).subscribe({
			next: () => { this.showSuccess(`Leave request for ${leave.employeeName} has been rejected.`); this.loadLeaves(); },
			error: response => this.showError(response, 'Unable to reject this leave request.')
		});
	}

	private showSuccess(text: string): void {
		this.messageTone.set('success');
		this.message.set(text);
	}

	private showError(response: { error?: { errors?: string[] } }, fallback: string): void {
		this.messageTone.set('error');
		this.message.set(response.error?.errors?.[0] ?? fallback);
	}
}

