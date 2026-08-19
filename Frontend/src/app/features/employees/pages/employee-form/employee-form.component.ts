import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

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
						<label class="form-label">Email</label>
						<input type="email" class="form-control" formControlName="email">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Phone</label>
						<input type="text" class="form-control" formControlName="phone">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">Department</label>
						<input type="text" class="form-control" formControlName="department">
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
							<option value="Monthly">Monthly</option>
							<option value="Bi-Weekly">Bi-Weekly</option>
							<option value="Weekly">Weekly</option>
						</select>
					</div>
					<div class="col-12 col-md-4">
						<label class="form-label">Basic Salary</label>
						<input type="number" class="form-control" formControlName="basicSalary">
					</div>

					@if (saved()) {
						<div class="col-12">
							<div class="alert alert-success mb-0">Employee profile saved successfully.</div>
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

	readonly saved = signal(false);
	readonly isEditMode = !!this.route.snapshot.paramMap.get('id');

	readonly form = this.fb.group({
		firstName: ['', Validators.required],
		lastName: ['', Validators.required],
		email: ['', [Validators.required, Validators.email]],
		phone: [''],
		department: ['', Validators.required],
		jobTitle: ['', Validators.required],
		startDate: ['', Validators.required],
		payFrequency: ['Monthly', Validators.required],
		basicSalary: [0, [Validators.required, Validators.min(1)]]
	});

	constructor() {
		if (this.isEditMode) {
			this.form.patchValue({
				firstName: 'Anele',
				lastName: 'Mokoena',
				email: 'anele@company.co.za',
				phone: '+27 82 441 2201',
				department: 'Finance',
				jobTitle: 'Senior Accountant',
				startDate: '2022-04-01',
				payFrequency: 'Monthly',
				basicSalary: 72500
			});
		}
	}

	onSubmit(): void {
		if (this.form.invalid) {
			return;
		}

		this.saved.set(true);
	}
}
