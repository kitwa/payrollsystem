import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../services/employee.service';
import { EmployeeList } from '../../models/employee.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
	selector: 'app-employee-list',
	standalone: true,
	imports: [CommonModule, RouterLink, PaginationComponent],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Employees</h1>
				<p class="text-muted mb-0">Manage employee records, status, and salary profile.</p>
			</div>
			<a class="btn btn-dark" routerLink="/employees/new"><i class="bi bi-person-plus me-2"></i>New Employee</a>
		</section>

		<section class="metrics mb-3">
			@for (metric of metrics(); track metric.label) {
				<article>
					<p>{{ metric.label }}</p>
					<strong>{{ metric.value }}</strong>
				</article>
			}
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="row g-2 mb-3">
					<div class="col-12 col-md-8">
						<label class="form-label">Search</label>
						<input
							type="text"
							class="form-control"
							placeholder="Name, employee number, or email"
							[value]="query()"
							(input)="setQuery($event)"
						>
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Department</label>
						<select class="form-select" [value]="department()" (change)="setDepartment($event)">
							@for (item of departments(); track item) {
								<option [value]="item">{{ item }}</option>
							}
						</select>
					</div>
				</div>

				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr>
								<th>Employee</th>
								<th>Department</th>
								<th>Job Title</th>
								<th>Status</th>
								<th class="text-end">Actions</th>
							</tr>
						</thead>
						<tbody>
							@for (employee of filteredEmployees(); track employee.id) {
								<tr>
									<td>
										<p class="mb-0 fw-semibold">{{ employee.firstName }} {{ employee.lastName }}</p>
										<small class="text-muted">{{ employee.employeeNumber }} · {{ employee.email }}</small>
									</td>
									<td>{{ employee.department }}</td>
									<td>{{ employee.jobTitle }}</td>
									<td>
										<span class="badge" [class.bg-success]="employee.status === 0" [class.bg-secondary]="employee.status !== 0">
											{{ statusName(employee.status) }}
										</span>
									</td>
									<td class="text-end">
										<a class="btn btn-sm btn-outline-secondary me-2" [routerLink]="['/employees', employee.id]">View</a>
										<a class="btn btn-sm btn-outline-dark" [routerLink]="['/employees', employee.id, 'edit']">Edit</a>
									</td>
								</tr>
							} @empty {
								<tr>
									<td colspan="5" class="text-center text-muted py-4">No employees found for this filter.</td>
								</tr>
							}
						</tbody>
					</table>
				</div>
				<app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="totalCount()" (pageChange)="loadPage($event)"></app-pagination>
			</div>
		</section>
	`,
	styles: [
		`
			.metrics {
				display: grid;
				grid-template-columns: repeat(4, minmax(0, 1fr));
				gap: 0.7rem;
			}

			.metrics article {
				border: 1px solid #e7e5e4;
				border-radius: 0.8rem;
				background: #fff;
				padding: 0.8rem;
			}

			.metrics p {
				margin: 0;
				color: #71717a;
				font-size: 0.82rem;
			}

			.metrics strong {
				display: block;
				margin-top: 0.2rem;
				font-size: 1.2rem;
			}

			@media (max-width: 920px) {
				.metrics {
					grid-template-columns: repeat(2, minmax(0, 1fr));
				}
			}

			@media (max-width: 520px) {
				.metrics {
					grid-template-columns: 1fr;
				}
			}
		`
	]
})
export class EmployeeListComponent {
	private readonly auth = inject(AuthService);
	private readonly employeeService = inject(EmployeeService);

	readonly query = signal('');
	readonly department = signal('All departments');
	readonly employees = signal<EmployeeList[]>([]);
	readonly pageNumber = signal(1);
	readonly pageSize = 20;
	readonly totalCount = signal(0);

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.loadPage(1);
		}
	}

	loadPage(page: number): void {
		const companyId = this.auth.companyId();
		if (!companyId || page < 1) return;
		this.employeeService.getAll(companyId, page, this.pageSize).subscribe(result => {
			this.pageNumber.set(result.pageNumber);
			this.totalCount.set(result.totalCount);
			this.employees.set(result.items);
		});
	}

	readonly departments = computed(() => [
		'All departments',
		...Array.from(new Set(this.employees().map(e => e.department).filter((d): d is string => !!d)))
	]);

	readonly filteredEmployees = computed(() => {
		const query = this.query().toLowerCase().trim();
		const department = this.department();

		return this.employees().filter(employee => {
			const matchesDepartment = department === 'All departments' || employee.department === department;
			const haystack = `${employee.firstName} ${employee.lastName} ${employee.employeeNumber} ${employee.email ?? ''}`.toLowerCase();
			const matchesSearch = !query || haystack.includes(query);
			return matchesDepartment && matchesSearch;
		});
	});

	readonly metrics = computed(() => {
		const all = this.employees();
		return [
			{ label: 'Total Employees', value: `${all.length}` },
			{ label: 'Active', value: `${all.filter(e => e.status === 0).length}` },
			{ label: 'On Leave', value: `${all.filter(e => e.status === 3).length}` },
			{ label: 'Terminated', value: `${all.filter(e => e.status === 1).length}` }
		];
	});

	statusName(status: number): string {
		return ['Active', 'Terminated', 'Suspended', 'On Leave'][status] ?? 'Unknown';
	}

	setQuery(event: Event): void {
		this.query.set((event.target as HTMLInputElement).value);
		this.pageNumber.set(1);
	}

	setDepartment(event: Event): void {
		this.department.set((event.target as HTMLSelectElement).value);
		this.pageNumber.set(1);
	}
}
