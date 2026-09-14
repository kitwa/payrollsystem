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

        <button type="button" class="btn btn-sm btn-outline-primary" (click)="openDepartmentModal()">
            + Add Department
        </button>
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
						<input type="number" step="0.01" min="0" class="form-control" formControlName="basicSalary">
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
					@if (isEditMode) {
						<div class="col-12"><hr><h2 class="h5 mb-2">Bank Account</h2></div>
						<div class="col-12 col-md-3"><label class="form-label">Bank Name</label><input class="form-control" formControlName="bankName"></div>
						<div class="col-12 col-md-3"><label class="form-label">Account Number</label><input class="form-control" formControlName="accountNumber"></div>
						<div class="col-12 col-md-3"><label class="form-label">Branch Code</label><input class="form-control" formControlName="branchCode"></div>
						<div class="col-12 col-md-3"><label class="form-label">Account Type</label><input class="form-control" formControlName="accountType"></div>
					} @else {
						<div class="col-12"><hr></div>
						<div class="col-12">
							<div class="form-check form-switch mb-2">
								<input class="form-check-input" type="checkbox" role="switch" id="allow-login-switch" formControlName="allowLogin">
								<label class="form-check-label" for="allow-login-switch">Allow employee self-service login</label>
							</div>
							@if (form.value.allowLogin) {
								<div class="row g-2 align-items-end">
									<div class="col-12 col-md-4">
										<label class="form-label">Login Role</label>
										<select class="form-select" formControlName="loginRole">
											<option value="Employee">Employee</option>
											<option value="PayrollManager">Payroll Manager</option>
										</select>
									</div>
									<div class="col-12 col-md-8">
										<small class="text-muted">An account will be created automatically and the employee will be emailed a link to set their own password.</small>
									</div>
								</div>
							}
						</div>
					}

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

		@if (showDepartmentModal()) {
			<div class="modal d-block" tabindex="-1" role="dialog" style="background:rgba(0,0,0,.5);">
				<div class="modal-dialog modal-dialog-centered modal-sm" role="document">
					<div class="modal-content">
						<div class="modal-header">
							<h2 class="modal-title h5 mb-0">Add Department</h2>
							<button type="button" class="btn-close" aria-label="Close" (click)="closeDepartmentModal()"></button>
						</div>
						<div class="modal-body">
							<label class="form-label">Department Name</label>
							<input class="form-control" [value]="newDepartmentName()" (input)="newDepartmentName.set($any($event.target).value)" (keyup.enter)="createDepartment()">
							@if (departmentError()) { <div class="alert alert-danger mt-2 mb-0 py-2 small">{{ departmentError() }}</div> }
						</div>
						<div class="modal-footer">
							<button type="button" class="btn btn-outline-secondary" (click)="closeDepartmentModal()">Cancel</button>
							<button type="button" class="btn btn-dark" [disabled]="creatingDepartment()" (click)="createDepartment()">{{ creatingDepartment() ? 'Creating…' : 'Create' }}</button>
						</div>
					</div>
				</div>
			</div>
		}
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
	readonly showDepartmentModal = signal(false);
	readonly newDepartmentName = signal('');
	readonly departmentError = signal('');
	readonly creatingDepartment = signal(false);
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
		status: [0, Validators.required],
		allowLogin: [false],
		loginRole: ['Employee'],
		bankName: [''],
		accountNumber: [''],
		branchCode: [''],
		accountType: ['']
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
						status: employee.status,
						bankName: employee.bankDetails?.bankName ?? '',
						accountNumber: employee.bankDetails?.accountNumber ?? '',
						branchCode: employee.bankDetails?.branchCode ?? '',
						accountType: employee.bankDetails?.accountType ?? ''
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
			}).subscribe({
				next: () => this.saveBankDetailsAndNavigate(value),
				error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to save employee.')
			});
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
				basicSalary: value.basicSalary!,
				allowLogin: value.allowLogin ?? false,
				loginRole: value.loginRole ?? 'Employee'
			}).subscribe({ next: () => this.router.navigate(['/employees']), error: response => this.setError(response, 'Unable to create employee.') });
	}

	private setError(response: { error?: { errors?: string[] } }, fallback: string): void {
		const message = response.error?.errors?.[0] ?? fallback;
		this.error.set(message);
		this.showUpgradeCta.set(message.toLowerCase().includes('upgrade') && (this.auth.isInRole('Admin') || this.auth.isInRole('SuperAdmin')));
	}

	openDepartmentModal(): void {
		this.newDepartmentName.set('');
		this.departmentError.set('');
		this.showDepartmentModal.set(true);
	}

	closeDepartmentModal(): void {
		this.showDepartmentModal.set(false);
	}

	createDepartment(): void {
		const companyId = this.auth.companyId();
		const name = this.newDepartmentName().trim();
		if (!companyId || !name || this.creatingDepartment()) return;

		this.creatingDepartment.set(true);
		this.departmentError.set('');
		this.departmentService.create(companyId, name).subscribe({
			next: () => {
				this.creatingDepartment.set(false);
				this.showDepartmentModal.set(false);
				this.departmentService.getAll(companyId).subscribe(departments => {
					this.departments.set(departments);
					this.form.patchValue({ department: name });
				});
			},
			error: response => {
				this.creatingDepartment.set(false);
				this.departmentError.set(response.error?.errors?.[0] ?? 'Unable to create department.');
			}
		});
	}

	private saveBankDetailsAndNavigate(value: ReturnType<typeof this.form.getRawValue>): void {
		const details = {
			bankName: value.bankName?.trim() ?? '',
			accountNumber: value.accountNumber?.trim() ?? '',
			branchCode: value.branchCode?.trim() ?? '',
			accountType: value.accountType?.trim() ?? ''
		};
		if (!details.bankName && !details.accountNumber && !details.branchCode && !details.accountType) {
			this.router.navigate(['/employees']);
			return;
		}
		this.employeeService.updateBankDetails(this.employeeId!, details).subscribe({
			next: () => this.router.navigate(['/employees']),
			error: response => this.setError(response, 'Unable to save bank details.')
		});
	}
}
