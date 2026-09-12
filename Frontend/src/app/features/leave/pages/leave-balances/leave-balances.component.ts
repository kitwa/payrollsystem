import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { LeaveService } from '../../services/leave.service';
import { EmployeeLeaveBalance } from '../../models/leave.models';

@Component({
	selector: 'app-leave-balances',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Leave Balances</h1>
				<p class="text-muted mb-0">See how many leave days each employee has remaining, by leave type.</p>
			</div>
			<a class="btn btn-outline-dark" routerLink="/leave">Back to Leave Requests</a>
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="row g-2 mb-3">
					<div class="col-12 col-md-3">
						<label class="form-label">Year</label>
						<select class="form-select" [value]="selectedYear()" (change)="setYear($event)">
							@for (year of years; track year) {
								<option [value]="year">{{ year }}</option>
							}
						</select>
					</div>
					<div class="col-12 col-md-5">
						<label class="form-label">Search employee</label>
						<input class="form-control" type="text" placeholder="Name or employee number" [value]="search()" (input)="setSearch($event)" />
					</div>
				</div>

				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr>
								<th>Employee</th>
								<th>Leave Balances</th>
							</tr>
						</thead>
						<tbody>
							@for (item of filteredBalances(); track item.employeeId) {
								<tr>
									<td>
										{{ item.employeeName }}
										<small class="text-muted d-block">{{ item.employeeNumber }}</small>
									</td>
									<td>
										<div class="d-flex flex-wrap gap-2">
											@for (balance of item.balances; track balance.leaveTypeId) {
												<span class="badge bg-light text-dark border">
													{{ balance.leaveTypeName }}: {{ balance.balanceDays }} / {{ balance.entitlementDays }} days
												</span>
											} @empty {
												<span class="text-muted">No leave types configured.</span>
											}
										</div>
									</td>
								</tr>
							} @empty {
								<tr><td colspan="2" class="text-center text-muted py-4">No employees found.</td></tr>
							}
						</tbody>
					</table>
				</div>
			</div>
		</section>
	`
})
export class LeaveBalancesComponent {
	private readonly auth = inject(AuthService);
	private readonly leaveService = inject(LeaveService);

	readonly currentYear = new Date().getFullYear();
	readonly years = [this.currentYear - 1, this.currentYear, this.currentYear + 1];
	readonly selectedYear = signal(this.currentYear);
	readonly search = signal('');
	readonly balances = signal<EmployeeLeaveBalance[]>([]);

	constructor() {
		this.load();
	}

	private load(): void {
		const companyId = this.auth.companyId();
		if (!companyId) return;
		this.leaveService.getCompanyBalances(companyId, this.selectedYear()).subscribe(result => this.balances.set(result));
	}

	readonly filteredBalances = computed(() => {
		const term = this.search().trim().toLowerCase();
		if (!term) return this.balances();
		return this.balances().filter(item =>
			item.employeeName.toLowerCase().includes(term) || item.employeeNumber.toLowerCase().includes(term));
	});

	setYear(event: Event): void {
		this.selectedYear.set(Number((event.target as HTMLSelectElement).value));
		this.load();
	}

	setSearch(event: Event): void {
		this.search.set((event.target as HTMLInputElement).value);
	}
}
