import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { LeaveService } from '../../services/leave.service';
import { SettingsService } from '../../../settings/services/settings.service';
import { LeaveType } from '../../../settings/models/settings.models';

@Component({
	selector: 'app-leave-request',
	standalone: true,
	imports: [CommonModule, ReactiveFormsModule, RouterLink],
	template: `
		<section class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
			<div>
				<h1 class="h3 mb-1">Create Leave Request</h1>
				<p class="text-muted mb-0">Capture leave type, date range, and business handover notes.</p>
			</div>
			<a class="btn btn-outline-secondary" routerLink="/leave">Back to Leave List</a>
		</section>

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<form class="row g-3" [formGroup]="form" (ngSubmit)="onSubmit()">
					<div class="col-12 col-md-6">
						<label class="form-label">Leave Type</label>
						<select class="form-select" formControlName="leaveTypeId">
							<option value="">{{ leaveTypes().length ? 'Select leave type' : 'No leave types available' }}</option>
							@for (type of leaveTypes(); track type.id) {
								<option [value]="type.id">{{ type.name }}</option>
							}
						</select>
						@if (error()) { <small class="text-danger">{{ error() }}</small> }
					</div>
					<div class="col-12 col-md-6"></div>
					<div class="col-12 col-md-6">
						<label class="form-label">Start Date</label>
						<input type="date" class="form-control" formControlName="startDate">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">End Date</label>
						<input type="date" class="form-control" formControlName="endDate">
						@if (totalDays() !== null) {
							<small class="text-muted d-block mt-1">
								Duration: <strong>{{ totalDays() }} {{ totalDays() === 1 ? 'day' : 'days' }}</strong>
							</small>
						}
					</div>
					<div class="col-12">
						<label class="form-label">Reason / Notes</label>
						<textarea rows="4" class="form-control" formControlName="reason" placeholder="Optional manager note"></textarea>
					</div>

					@if (submitted()) {
						<div class="col-12">
							<div class="alert alert-success mb-0">Leave request has been submitted for manager approval.</div>
						</div>
					}

					<div class="col-12 d-flex justify-content-end">
						<button class="btn btn-dark" type="submit" [disabled]="form.invalid">Submit Request</button>
					</div>
				</form>
			</div>
		</section>
	`
})
export class LeaveRequestComponent {
	private readonly fb = inject(FormBuilder);
	private readonly auth = inject(AuthService);
	private readonly leaveService = inject(LeaveService);
	private readonly settingsService = inject(SettingsService);

	readonly submitted = signal(false);
	readonly error = signal('');
	readonly leaveTypes = signal<LeaveType[]>([]);
	readonly totalDays = signal<number | null>(null);

	readonly form = this.fb.group({
		leaveTypeId: ['', Validators.required],
		startDate: ['', Validators.required],
		endDate: ['', Validators.required],
		reason: ['']
	});

	constructor() {
		this.form.valueChanges.subscribe(val => {
			this.updateTotalDays(val.startDate, val.endDate);
		});

		const companyId = this.auth.companyId();
		if (companyId) {
			this.settingsService.getLeaveTypes(companyId).subscribe({
				next: types => {
					this.leaveTypes.set(types);
					if (types.length) this.form.patchValue({ leaveTypeId: types[0].id });
				},
				error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to load leave types.')
			});
		}
	}

	private updateTotalDays(startDate?: string | null, endDate?: string | null): void {
		if (!startDate || !endDate) {
			this.totalDays.set(null);
			return;
		}
		const start = new Date(startDate);
		const end = new Date(endDate);
		if (isNaN(start.getTime()) || isNaN(end.getTime()) || end < start) {
			this.totalDays.set(null);
			return;
		}
		const diffTime = end.getTime() - start.getTime();
		const days = Math.round(diffTime / (1000 * 60 * 60 * 24)) + 1;
		this.totalDays.set(days);
	}

	onSubmit(): void {
		const employeeId = this.auth.employeeId();
		if (this.form.invalid || !employeeId) {
			return;
		}

		const value = this.form.getRawValue();
		this.leaveService.request({
			employeeId,
			leaveTypeId: value.leaveTypeId!,
			startDate: value.startDate!,
			endDate: value.endDate!,
			reason: value.reason ?? undefined
		}).subscribe(() => this.submitted.set(true));
	}
}

