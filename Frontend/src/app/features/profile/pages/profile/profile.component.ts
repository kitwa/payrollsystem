import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../../employees/services/employee.service';
import { SettingsService } from '../../../settings/services/settings.service';
import { Employee } from '../../../employees/models/employee.models';
import { Company } from '../../../settings/models/settings.models';

@Component({
	selector: 'app-profile',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="mb-3">
			<h1 class="h3 mb-1">My Profile</h1>
			<p class="text-muted mb-0">View your account details. Contact an administrator to make changes.</p>
		</section>

		<section class="row g-3">
			<div class="col-12 col-lg-6">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Account</h2>
						<dl class="row mb-0">
							<dt class="col-5">Full Name</dt><dd class="col-7">{{ user()?.firstName }} {{ user()?.lastName }}</dd>
							<dt class="col-5">Email</dt><dd class="col-7">{{ user()?.email }}</dd>
							<dt class="col-5">Role(s)</dt><dd class="col-7">{{ roleNames() }}</dd>
							<dt class="col-5">Company</dt><dd class="col-7">{{ company()?.name || '—' }}</dd>
						</dl>
					</div>
				</article>
			</div>

			<div class="col-12 col-lg-6">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Employment Details</h2>
						@if (employee(); as e) {
							<dl class="row mb-0">
								<dt class="col-5">Employee Number</dt><dd class="col-7">{{ e.employeeNumber }}</dd>
								<dt class="col-5">Job Title</dt><dd class="col-7">{{ e.jobTitle || '—' }}</dd>
								<dt class="col-5">Department</dt><dd class="col-7">{{ e.department || '—' }}</dd>
								<dt class="col-5">Phone</dt><dd class="col-7">{{ e.phone || '—' }}</dd>
								<dt class="col-5">Address</dt><dd class="col-7">{{ e.address || '—' }}</dd>
								<dt class="col-5">Start Date</dt><dd class="col-7">{{ e.startDate | date:'yyyy-MM-dd' }}</dd>
							</dl>
						} @else {
							<p class="text-muted small mb-0">No employee record is linked to this account.</p>
						}
					</div>
				</article>
			</div>
		</section>

		<section class="row g-3 mt-1">
			<div class="col-12">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-2">Security</h2>
						<p class="text-muted small mb-3">Update the password used to sign in to your account.</p>
						<a class="btn btn-outline-dark" routerLink="/change-password">Change Password</a>
					</div>
				</article>
			</div>
		</section>
	`
})
export class ProfileComponent {
	private readonly auth = inject(AuthService);
	private readonly employeeService = inject(EmployeeService);
	private readonly settingsService = inject(SettingsService);

	readonly user = this.auth.currentUser;
	readonly employee = signal<Employee | null>(null);
	readonly company = signal<Company | null>(null);

	readonly roleNames = computed(() => this.user()?.roles?.join(', ') || '—');

	constructor() {
		if (this.auth.employeeId()) {
			this.employeeService.getMine().subscribe({ next: employee => this.employee.set(employee) });
		}
		const companyId = this.auth.companyId();
		if (companyId && (this.auth.isInRole('Admin') || this.auth.isInRole('SuperAdmin'))) {
			this.settingsService.getCompany(companyId).subscribe({ next: company => this.company.set(company) });
		}
	}
}
