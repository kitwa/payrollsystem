import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/auth/auth.service';
import { SettingsService } from '../../services/settings.service';
import { LeaveType } from '../../models/settings.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
	selector: 'app-leave-types',
	standalone: true,
	imports: [CommonModule, PaginationComponent],
	template: `
		<section class="mb-3 d-flex justify-content-between align-items-start flex-wrap gap-2">
			<div>
				<h1 class="h3 mb-1">Leave Types</h1>
				<p class="text-muted mb-0">Define leave categories, accrual rates, and policy eligibility.</p>
			</div>
			<button class="btn btn-dark" type="button" (click)="addLeaveType()">Add Leave Type</button>
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr>
								<th>Name</th>
								<th>Days / Year</th>
								<th>Paid</th>
								<th>Status</th>
								<th></th>
							</tr>
						</thead>
						<tbody>
							@for (item of pagedLeaveTypes(); track item.id) {
								<tr>
									<td>{{ item.name }}</td>
									<td>{{ item.defaultEntitlementDays }}</td>
									<td>{{ item.isPaid ? 'Paid' : 'Unpaid' }}</td>
									<td><span class="badge" [class.bg-success]="item.isActive" [class.bg-secondary]="!item.isActive">{{ item.isActive ? 'Active' : 'Disabled' }}</span></td>
									<td class="text-end"><button class="btn btn-sm btn-outline-secondary" type="button" (click)="toggleActive(item)">{{ item.isActive ? 'Disable' : 'Enable' }}</button></td>
								</tr>
							} @empty {
								<tr><td colspan="5" class="text-center text-muted py-4">No leave types configured.</td></tr>
							}
						</tbody>
					</table>
				</div>
				<app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="leaveTypes().length" (pageChange)="pageNumber.set($event)"></app-pagination>
			</div>
		</section>
	`
})
export class LeaveTypesComponent {
	private readonly auth = inject(AuthService);
	private readonly settingsService = inject(SettingsService);

	readonly leaveTypes = signal<LeaveType[]>([]);
	readonly pageNumber = signal(1);
	readonly pageSize = 20;
	readonly pagedLeaveTypes = computed(() => this.leaveTypes().slice((this.pageNumber() - 1) * this.pageSize, this.pageNumber() * this.pageSize));

	constructor() {
		this.load();
	}

	private load(): void {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.settingsService.getLeaveTypes(companyId).subscribe(types => this.leaveTypes.set(types));
		}
	}

	addLeaveType(): void {
		const companyId = this.auth.companyId();
		if (!companyId) return;
		this.settingsService.createLeaveType({
			companyId, name: 'New Leave Type', defaultEntitlementDays: 0, isPaid: true, requiresApproval: true
		}).subscribe(() => this.load());
	}

	toggleActive(item: LeaveType): void {
		this.settingsService.updateLeaveType(item.id, {
			name: item.name,
			defaultEntitlementDays: item.defaultEntitlementDays,
			isPaid: item.isPaid,
			requiresApproval: item.requiresApproval,
			isActive: !item.isActive
		}).subscribe(() => this.load());
	}
}

