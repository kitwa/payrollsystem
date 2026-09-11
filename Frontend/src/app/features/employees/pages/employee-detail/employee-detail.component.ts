import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EmployeeService } from '../../services/employee.service';
import { Employee } from '../../models/employee.models';

@Component({
	selector: 'app-employee-detail',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<p class="text-muted mb-1">Employee Profile</p>
				<h1 class="h3 mb-0">{{ employee()?.firstName ?? 'Employee' }} {{ employee()?.lastName ?? '' }}</h1>
			</div>
			<div class="d-flex gap-2">
				<a class="btn btn-outline-secondary" routerLink="/employees">Back</a>
				@if (employee(); as employee) {
					<a class="btn btn-dark" [routerLink]="['/employees', employee.id, 'edit']">Edit Employee</a>
				}
			</div>
		</section>

		@if (error()) {
			<div class="alert alert-danger">{{ error() }}</div>
		}

		@if (!employee() && !error()) {
			<div class="alert alert-light border">Loading employee profile…</div>
		}

		@if (employee(); as employee) {

		<section class="row g-3">
			<div class="col-12 col-lg-8">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Personal Information</h2>
						<div class="info-grid">
							<div><span>ID Number</span><strong>{{ employee.idNumber }}</strong></div>
							<div><span>Email</span><strong>{{ employee.email ?? 'Not provided' }}</strong></div>
							<div><span>Phone</span><strong>{{ employee.phone ?? 'Not provided' }}</strong></div>
							<div><span>Department</span><strong>{{ employee.department ?? 'Unassigned' }}</strong></div>
							<div><span>Job Title</span><strong>{{ employee.jobTitle ?? 'Not provided' }}</strong></div>
							<div><span>Employment Type</span><strong>{{ employmentTypeLabel(employee.employmentType) }}</strong></div>
						</div>
					</div>
				</article>
			</div>

			<div class="col-12 col-lg-4">
				<article class="card border-0 shadow-sm mb-3">
					<div class="card-body">
						<h2 class="h6 text-muted">Bank Account</h2>
						@if (employee.bankDetails; as bank) {
							<ul class="list-unstyled mb-0 small"><li><strong>Bank:</strong> {{ bank.bankName }}</li><li><strong>Account:</strong> {{ bank.accountNumber }}</li><li><strong>Branch:</strong> {{ bank.branchCode }}</li><li><strong>Type:</strong> {{ bank.accountType }}</li></ul>
						} @else {
							<p class="text-muted small mb-0">No bank details captured.</p>
						}
					</div>
				</article>
				<article class="card border-0 shadow-sm mb-3">
					<div class="card-body">
						<h2 class="h6 text-muted">Compensation</h2>
						<p class="salary">R {{ employee.basicSalary | number:'1.0-0' }}</p>
						<p class="mb-0 text-muted">{{ payFrequencyLabel(employee.payFrequency) }}</p>
					</div>
				</article>
				<article class="card border-0 shadow-sm">
					<div class="card-body">
						<h2 class="h6 text-muted">Status</h2>
						<span class="badge bg-success mb-3">{{ statusLabel(employee.status) }}</span>
						<ul class="list-unstyled mb-0 text-muted small">
							<li>Start date: {{ employee.startDate | date:'yyyy-MM-dd' }}</li>
							<li>Tax number: {{ employee.taxNumber ?? 'Not provided' }}</li>
							<li>UIF number: {{ employee.uifNumber ?? 'Not provided' }}</li>
						</ul>
					</div>
				</article>
			</div>
		</section>
		}
	`,
	styles: [
		`
			.info-grid {
				display: grid;
				grid-template-columns: repeat(2, minmax(0, 1fr));
				gap: 0.75rem;
			}

			.info-grid span {
				display: block;
				font-size: 0.8rem;
				color: #71717a;
			}

			.info-grid strong {
				font-size: 0.96rem;
			}

			.salary {
				font-size: 1.7rem;
				margin: 0;
				font-weight: 700;
			}

			@media (max-width: 640px) {
				.info-grid {
					grid-template-columns: 1fr;
				}
			}
		`
	]
})
export class EmployeeDetailComponent {
	private readonly route = inject(ActivatedRoute);
	private readonly employeeService = inject(EmployeeService);

	readonly employee = signal<Employee | null>(null);
	readonly error = signal('');

	constructor() {
		const id = this.route.snapshot.paramMap.get('id');
		if (!id) {
			this.error.set('Employee id is missing.');
			return;
		}

		this.employeeService.getById(id).subscribe({
			next: employee => this.employee.set(employee),
			error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load employee profile.')
		});
	}

	statusLabel(status: number): string {
		return ['Active', 'Terminated', 'Suspended', 'On Leave'][status] ?? 'Unknown';
	}

	payFrequencyLabel(value: number): string {
		return ['Monthly', 'Weekly', 'Fortnightly'][value] ?? 'Unknown';
	}

	employmentTypeLabel(value: number): string {
		return ['Permanent', 'Contract', 'Part time', 'Casual', 'Intern'][value] ?? 'Unknown';
	}

}
