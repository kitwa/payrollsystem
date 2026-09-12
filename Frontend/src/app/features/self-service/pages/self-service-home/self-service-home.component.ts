import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { computed, inject, signal } from '@angular/core';
import { EmployeeService } from '../../../employees/services/employee.service';
import { Employee } from '../../../employees/models/employee.models';
import { LeaveService } from '../../../leave/services/leave.service';
import { LeaveBalance } from '../../../leave/models/leave.models';

@Component({
	selector: 'app-self-service-home',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="mb-3">
			<h1 class="h3 mb-1">Employee Self-Service</h1>
			<p class="text-muted mb-0">Access your payslips, leave balance, and personal information updates.</p>
		</section>

		<section class="row g-3 mb-3">
			@for (card of summaryCards(); track card.label) {
				<div class="col-12 col-md-4">
					<article class="card border-0 shadow-sm h-100">
						<div class="card-body">
							<p class="text-muted mb-1">{{ card.label }}</p>
							<h2 class="h4 mb-0">{{ card.value }}</h2>
						</div>
					</article>
				</div>
			}
		</section>

		<section class="row g-3">
			<div class="col-12 col-lg-6">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Quick Actions</h2>
						<div class="d-grid gap-2">
							<a class="btn btn-outline-dark" routerLink="/payslips">View Payslips</a>
							<a class="btn btn-outline-dark" routerLink="/leave/request">Request Leave</a>
							<h3 class="h6 mt-4">Banking Details</h3>
							@if (employee(); as profile) {
								@if (profile.bankDetails; as bank) {
									<dl class="row small mb-0"><dt class="col-5">Bank</dt><dd class="col-7">{{ bank.bankName }}</dd><dt class="col-5">Account</dt><dd class="col-7">{{ bank.accountNumber }}</dd><dt class="col-5">Branch</dt><dd class="col-7">{{ bank.branchCode }}</dd><dt class="col-5">Type</dt><dd class="col-7">{{ bank.accountType }}</dd></dl>
								} @else { <p class="text-muted small mb-0">No bank details have been captured.</p> }
							} @else { <p class="text-muted small mb-0">Loading bank details...</p> }
						</div>
					</div>
				</article>
			</div>

			<div class="col-12 col-lg-6">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Leave Balances ({{ currentYear }})</h2>
						@if (leaveBalances().length) {
							<ul class="list-group list-group-flush">
								@for (balance of leaveBalances(); track balance.leaveTypeId) {
									<li class="list-group-item px-0 d-flex justify-content-between">
										<span>{{ balance.leaveTypeName }}</span>
										<span>{{ balance.balanceDays }} / {{ balance.entitlementDays }} days</span>
									</li>
								}
							</ul>
						} @else {
							<p class="text-muted small mb-0">No leave balances found for this year.</p>
						}
					</div>
				</article>
			</div>
		</section>

		<section class="row g-3 mt-1">
			<div class="col-12">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Latest Notifications</h2>
						<ul class="list-group list-group-flush">
							<li class="list-group-item px-0">August payslip is now available.</li>
							<li class="list-group-item px-0">Leave request #LV-01 awaiting manager approval.</li>
							<li class="list-group-item px-0">Tax certificate preview available for review.</li>
						</ul>
					</div>
				</article>
			</div>
		</section>
	`
})
export class SelfServiceHomeComponent {
	private readonly employeeService = inject(EmployeeService);
	private readonly leaveService = inject(LeaveService);
	readonly employee = signal<Employee | null>(null);
	readonly leaveBalances = signal<LeaveBalance[]>([]);
	readonly currentYear = new Date().getFullYear();

	readonly totalLeaveBalance = computed(() =>
		this.leaveBalances().reduce((sum, balance) => sum + balance.balanceDays, 0));

	readonly summaryCards = computed(() => [
		{ label: 'Leave Balance', value: `${this.totalLeaveBalance()} days` },
		{ label: 'Next Payday', value: '29 Aug 2026' },
		{ label: 'Latest Net Pay', value: 'R 41,520' }
	]);

	constructor() {
		this.employeeService.getMine().subscribe({ next: employee => this.employee.set(employee) });
		this.leaveService.getMyBalances().subscribe({ next: balances => this.leaveBalances.set(balances) });
	}
}
