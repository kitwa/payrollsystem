import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { EmployeeService } from '../../services/employee.service';
import { Department, Employee } from '../../models/employee.models';
import { DepartmentService } from '../../services/employee.service';

@Component({
	selector: 'app-employee-form',
	standalone: true,
	imports: [CommonModule, ReactiveFormsModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">{{ isEditMode ? 'Edit Employee' : 'Add New Employee' }}</h1>
				<p class="text-muted mb-0">Capture complete payroll-ready employee information.</p>
			</div>
			<a class="btn btn-outline-secondary" routerLink="/employees">Cancel</a>
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<form class="row g-3" [formGroup]="form" (ngSubmit)="onSubmit()">
					<div class="col-12 col-md-6">
						<label class="form-label">First Name</label>
						<input type="text" class="form-control" formControlName="firstName">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Last Name</label>
						<input type="text" class="form-control" formControlName="lastName">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">ID Number</label>
						<input type="text" class="form-control" formControlName="idNumber">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Date of Birth</label>
						<input type="date" class="form-control" formControlName="dateOfBirth">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Gender</label>
						<select class="form-select" formControlName="gender">
							<option [ngValue]="0">Male</option>
							<option [ngValue]="1">Female</option>
							<option [ngValue]="2">Other</option>
							<option [ngValue]="3">Prefer not to say</option>
						</select>
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Email</label>
						<input type="email" class="form-control" formControlName="email">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Phone</label>
						<input type="text" class="form-control" formControlName="phone">
					</div>
<div class="col-12 col-md-6">
    <div class="d-flex justify-content-between align-items-center mb-1">
        <label class="form-label mb-0">Department</label>

        <a
            routerLink="/settings/departments"
            class="btn btn-sm btn-outline-primary"
        >
            + Add Department
        </a>
    </div>

    <select class="form-select" formControlName="department">
        <option value="">Select department</option>

        @for (department of departments(); track department.id) {
            <option [value]="department.name">
                {{ department.name }}
            </option>
        }
    </select>
</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Job Title</label>
						<input type="text" class="form-control" formControlName="jobTitle">
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Start Date</label>
						<input type="date" class="form-control" formControlName="startDate">
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Pay Frequency</label>
						<select class="form-select" formControlName="payFrequency">
							<option [ngValue]="0">Monthly</option>
							<option [ngValue]="1">Weekly</option>
							<option [ngValue]="2">Fortnightly</option>
						</select>
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Employment Type</label>
						<select class="form-select" formControlName="employmentType">
							<option [ngValue]="0">Permanent</option>
							<option [ngValue]="1">Contract</option>
							<option [ngValue]="2">Part time</option>
							<option [ngValue]="3">Casual</option>
							<option [ngValue]="4">Intern</option>
						</select>
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Basic Salary</label>
						<input type="number" class="form-control" formControlName="basicSalary">
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Status</label>
						<select class="form-select" formControlName="status">
							<option [ngValue]="0">Active</option>
							<option [ngValue]="2">Suspended</option>
							<option [ngValue]="3">On leave</option>
							<option [ngValue]="1">Terminated</option>
						</select>
					</div>

					@if (saved()) {
						<div class="col-12">
							<div class="alert alert-success mb-0">Employee profile saved successfully.</div>
						</div>
					}
					@if (error()) {
						<div class="col-12">
							<div class="alert alert-danger mb-0 d-flex justify-content-between align-items-center flex-wrap gap-2">
								<span>{{ error() }}</span>
								@if (showUpgradeCta()) {
									<a routerLink="/billing" class="btn btn-sm btn-dark">Upgrade Plan</a>
								}
							</div>
						</div>
					}

					<div class="col-12 d-flex justify-content-end">
						<button class="btn btn-dark" type="submit" [disabled]="form.invalid">
							{{ isEditMode ? 'Save Changes' : 'Create Employee' }}
						</button>
					</div>
				</form>
			</div>
		</section>
	`
})
export class EmployeeFormComponent {
	private readonly fb = inject(FormBuilder);
	private readonly route = inject(ActivatedRoute);
	private readonly router = inject(Router);
	private readonly auth = inject(AuthService);
	private readonly employeeService = inject(EmployeeService);
	private readonly departmentService = inject(DepartmentService);

	readonly saved = signal(false);
	readonly error = signal('');
	readonly showUpgradeCta = signal(false);
	readonly departments = signal<Department[]>([]);
	readonly isEditMode = !!this.route.snapshot.paramMap.get('id');
	readonly employeeId = this.route.snapshot.paramMap.get('id');
	private employee: Employee | null = null;

	readonly form = this.fb.group({
		firstName: ['', Validators.required],
		lastName: ['', Validators.required],
		idNumber: ['', Validators.required],
		dateOfBirth: ['', Validators.required],
		gender: [3, Validators.required],
		email: ['', [Validators.required, Validators.email]],
		phone: [''],
		department: [''],
		jobTitle: ['', Validators.required],
		startDate: ['', Validators.required],
		payFrequency: [0, Validators.required],
		employmentType: [0, Validators.required],
		basicSalary: [0, [Validators.required, Validators.min(1)]],
		status: [0, Validators.required]
	});

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.departmentService.getAll(companyId).subscribe(departments => this.departments.set(departments));
		}
		if (this.isEditMode) {
			if (this.employeeId) {
				this.employeeService.getById(this.employeeId).subscribe(employee => {
					this.employee = employee;
					this.form.patchValue({
						firstName: employee.firstName,
						lastName: employee.lastName,
						idNumber: employee.idNumber,
						dateOfBirth: employee.dateOfBirth.substring(0, 10),
						gender: employee.gender,
						email: employee.email ?? '',
						phone: employee.phone ?? '',
						department: employee.department ?? '',
						jobTitle: employee.jobTitle ?? '',
						startDate: employee.startDate.substring(0, 10),
						payFrequency: employee.payFrequency,
						employmentType: employee.employmentType,
						basicSalary: employee.basicSalary,
						status: employee.status
					});
				});
			}
		}
	}

	onSubmit(): void {
		if (this.form.invalid) {
			return;
		}
		this.error.set('');
		this.showUpgradeCta.set(false);

		const value = this.form.getRawValue();
		if (this.isEditMode && this.employeeId && this.employee) {
			this.employeeService.update(this.employeeId, {
				firstName: value.firstName!,
				lastName: value.lastName!,
				email: value.email!,
				phone: value.phone ?? undefined,
				address: this.employee.address,
				taxNumber: this.employee.taxNumber,
				jobTitle: value.jobTitle ?? undefined,
				department: value.department ?? undefined,
				basicSalary: value.basicSalary!,
				status: this.employee.status
			}).subscribe({ next: () => this.router.navigate(['/employees']), error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to save employee.') });
			return;
		}

		const companyId = this.auth.companyId();
		if (!companyId) return;
		this.employeeService.create({
			companyId,
			firstName: value.firstName!,
			lastName: value.lastName!,
			idNumber: value.idNumber!,
			dateOfBirth: value.dateOfBirth!,
			gender: value.gender!,
			email: value.email!,
			phone: value.phone ?? undefined,
			employmentType: value.employmentType!,
			payFrequency: value.payFrequency!,
			jobTitle: value.jobTitle ?? undefined,
			department: value.department?.trim() || undefined,
			startDate: value.startDate!,
				basicSalary: value.basicSalary!
			}).subscribe({ next: () => this.router.navigate(['/employees']), error: response => this.setError(response, 'Unable to create employee.') });
	}

	private setError(response: { error?: { errors?: string[] } }, fallback: string): void {
		const message = response.error?.errors?.[0] ?? fallback;
		this.error.set(message);
		this.showUpgradeCta.set(message.toLowerCase().includes('upgrade') && (this.auth.isInRole('Admin') || this.auth.isInRole('SuperAdmin')));
	}
}
