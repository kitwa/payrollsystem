import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
	selector: 'app-employee-detail',
	standalone: true,
	imports: [CommonModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<p class="text-muted mb-1">Employee Profile</p>
				<h1 class="h3 mb-0">{{ employee.firstName }} {{ employee.lastName }}</h1>
			</div>
			<div class="d-flex gap-2">
				<a class="btn btn-outline-secondary" routerLink="/employees">Back</a>
				<a class="btn btn-dark" [routerLink]="['/employees', employee.id, 'edit']">Edit Employee</a>
			</div>
		</section>

		<section class="row g-3">
			<div class="col-12 col-lg-8">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Personal Information</h2>
						<div class="info-grid">
							<div><span>ID Number</span><strong>{{ employee.idNumber }}</strong></div>
							<div><span>Email</span><strong>{{ employee.email }}</strong></div>
							<div><span>Phone</span><strong>{{ employee.phone }}</strong></div>
							<div><span>Department</span><strong>{{ employee.department }}</strong></div>
							<div><span>Job Title</span><strong>{{ employee.jobTitle }}</strong></div>
							<div><span>Employment Type</span><strong>{{ employee.employmentType }}</strong></div>
						</div>
					</div>
				</article>
			</div>

			<div class="col-12 col-lg-4">
				<article class="card border-0 shadow-sm mb-3">
					<div class="card-body">
						<h2 class="h6 text-muted">Compensation</h2>
						<p class="salary">R {{ employee.basicSalary | number:'1.0-0' }}</p>
						<p class="mb-0 text-muted">{{ employee.payFrequency }}</p>
					</div>
				</article>
				<article class="card border-0 shadow-sm">
					<div class="card-body">
						<h2 class="h6 text-muted">Status</h2>
						<span class="badge bg-success mb-3">{{ employee.status }}</span>
						<ul class="list-unstyled mb-0 text-muted small">
							<li>Start date: {{ employee.startDate }}</li>
							<li>Tax number: {{ employee.taxNumber }}</li>
							<li>UIF number: {{ employee.uifNumber }}</li>
						</ul>
					</div>
				</article>
			</div>
		</section>
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

	readonly employee = {
		id: this.route.snapshot.paramMap.get('id') ?? 'emp-001',
		firstName: 'Anele',
		lastName: 'Mokoena',
		idNumber: '9101025800081',
		email: 'anele@company.co.za',
		phone: '+27 82 441 2201',
		department: 'Finance',
		jobTitle: 'Senior Accountant',
		employmentType: 'Permanent',
		basicSalary: 72500,
		payFrequency: 'Monthly',
		status: 'Active',
		startDate: '2022-04-01',
		taxNumber: '9512/335/22/1',
		uifNumber: 'UIF-773311'
	};

}
