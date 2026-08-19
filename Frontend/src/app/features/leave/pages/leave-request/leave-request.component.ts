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
							@for (type of leaveTypes(); track type.id) {
								<option [value]="type.id">{{ type.name }}</option>
							}
						</select>
					</div>
					<div class="col-12 col-md-6"></div>
					<div class="col-12 col-md-6">
						<label class="form-label">Start Date</label>
						<input type="date" class="form-control" formControlName="startDate">
					</div>
					<div class="col-12 col-md-6">
						<label class="form-label">End Date</label>
						<input type="date" class="form-control" formControlName="endDate">
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
	readonly leaveTypes = signal<LeaveType[]>([]);

	readonly form = this.fb.group({
		leaveTypeId: ['', Validators.required],
		startDate: ['', Validators.required],
		endDate: ['', Validators.required],
		reason: ['']
	});

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.settingsService.getLeaveTypes(companyId).subscribe(types => {
				this.leaveTypes.set(types);
				if (types.length) this.form.patchValue({ leaveTypeId: types[0].id });
			});
		}
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

