import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { LeaveService } from '../../services/leave.service';
import { LeaveRequest, LeaveStatus } from '../../models/leave.models';

@Component({
	selector: 'app-leave-list',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Leave Requests</h1>
				<p class="text-muted mb-0">Review employee leave, balances, and manager approvals.</p>
			</div>
			<a class="btn btn-dark" routerLink="/leave/request">New Leave Request</a>
		</section>

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
							</tr>
						</thead>
						<tbody>
							@for (leave of filteredLeaves(); track leave.id) {
								<tr>
									<td>{{ leave.employeeName }}</td>
									<td>{{ leave.leaveTypeName }}</td>
									<td>{{ leave.startDate | date:'yyyy-MM-dd' }} to {{ leave.endDate | date:'yyyy-MM-dd' }}</td>
									<td>{{ leave.days }}</td>
									<td><span class="badge" [class.bg-success]="leave.status === 1" [class.bg-warning]="leave.status === 0" [class.bg-danger]="leave.status === 2">{{ statusName(leave.status) }}</span></td>
								</tr>
							} @empty {
								<tr><td colspan="5" class="text-center text-muted py-4">No leave requests found.</td></tr>
							}
						</tbody>
					</table>
				</div>
			</div>
		</section>
	`
})
export class LeaveListComponent {
	private readonly auth = inject(AuthService);
	private readonly leaveService = inject(LeaveService);

	readonly statuses = ['All', 'Pending', 'Approved', 'Rejected'];
	readonly selectedStatus = signal('All');
	readonly leaveRequests = signal<LeaveRequest[]>([]);

	constructor() {
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

	statusName(status: LeaveStatus): string {
		return LeaveStatus[status];
	}

	setStatus(event: Event): void {
		this.selectedStatus.set((event.target as HTMLSelectElement).value);
	}
}

