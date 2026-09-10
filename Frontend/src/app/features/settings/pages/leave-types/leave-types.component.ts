import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { SettingsService } from '../../services/settings.service';
import { LeaveType } from '../../models/settings.models';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';

@Component({
	selector: 'app-leave-types',
	standalone: true,
	imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
	template: `
		<section class="mb-3 d-flex justify-content-between align-items-start flex-wrap gap-2">
			<div>
				<h1 class="h3 mb-1">Leave Types</h1>
				<p class="text-muted mb-0">Define leave categories, accrual rates, and policy eligibility.</p>
			</div>
			<button class="btn btn-dark" type="button" (click)="startAdd()">Add Leave Type</button>
		</section>

		@if (error()) { <div class="alert alert-danger">{{ error() }}</div> }

		@if (formMode(); as mode) {
			<section class="card border-0 shadow-sm mb-3">
				<div class="card-body">
					<h2 class="h5 mb-3">{{ mode === 'add' ? 'Add Leave Type' : 'Edit Leave Type' }}</h2>
					<form class="row g-3" [formGroup]="form" (ngSubmit)="save()">
						<div class="col-12 col-md-4">
							<label class="form-label">Name</label>
							<input class="form-control" formControlName="name">
						</div>
						<div class="col-12 col-md-3">
							<label class="form-label">Days / Year</label>
							<input class="form-control" type="number" formControlName="defaultEntitlementDays">
						</div>
						<div class="col-12 col-md-2">
							<label class="form-label">Paid</label>
							<select class="form-select" formControlName="isPaid">
								<option [ngValue]="true">Yes</option>
								<option [ngValue]="false">No</option>
							</select>
						</div>
						<div class="col-12 col-md-3">
							<label class="form-label">Requires Approval</label>
							<select class="form-select" formControlName="requiresApproval">
								<option [ngValue]="true">Yes</option>
								<option [ngValue]="false">No</option>
							</select>
						</div>
						<div class="col-12 d-flex justify-content-end gap-2">
							<button class="btn btn-outline-secondary" type="button" (click)="cancel()">Cancel</button>
							<button class="btn btn-dark" type="submit" [disabled]="form.invalid">Save</button>
						</div>
					</form>
				</div>
			</section>
		}

		<section class="card border-0 shadow-sm">
			<div class="card-body">
				<div class="table-responsive">
					<table class="table align-middle mb-0">
						<thead>
							<tr>
								<th>Name</th>
								<th>Days / Year</th>
								<th>Paid</th>
								<th>Requires Approval</th>
								<th>Status</th>
								<th></th>
							</tr>
						</thead>
						<tbody>
							@for (item of pagedLeaveTypes(); track item.id) {
								<tr>
									<td>{{ item.name }}</td>
									<td>{{ item.defaultEntitlementDays }}</td>
									<td>{{ item.isPaid ? 'Yes' : 'No' }}</td>
									<td>{{ item.requiresApproval ? 'Yes' : 'No' }}</td>
									<td><span class="badge" [class.bg-success]="item.isActive" [class.bg-secondary]="!item.isActive">{{ item.isActive ? 'Active' : 'Disabled' }}</span></td>
									<td class="text-end d-flex gap-2 justify-content-end">
										<button class="btn btn-sm btn-outline-secondary" type="button" (click)="startEdit(item)">Edit</button>
										<button class="btn btn-sm btn-outline-secondary" type="button" (click)="toggleActive(item)">{{ item.isActive ? 'Disable' : 'Enable' }}</button>
									</td>
								</tr>
							} @empty {
								<tr><td colspan="6" class="text-center text-muted py-4">No leave types configured.</td></tr>
							}
						</tbody>
					</table>
				</div>
				<app-pagination [page]="pageNumber()" [pageSize]="pageSize" [total]="leaveTypes().length" (pageChange)="pageNumber.set($event)"></app-pagination>
			</div>
		</section>
	`
})
export class LeaveTypesComponent {
	private readonly fb = inject(FormBuilder);
	private readonly auth = inject(AuthService);
	private readonly settingsService = inject(SettingsService);

	readonly leaveTypes = signal<LeaveType[]>([]);
	readonly pageNumber = signal(1);
	readonly pageSize = 20;
	readonly pagedLeaveTypes = computed(() => this.leaveTypes().slice((this.pageNumber() - 1) * this.pageSize, this.pageNumber() * this.pageSize));

	readonly error = signal('');
	readonly formMode = signal<'add' | 'edit' | null>(null);
	private editingId: string | null = null;

	readonly form = this.fb.group({
		name: ['', Validators.required],
		defaultEntitlementDays: [0, [Validators.required, Validators.min(0)]],
		isPaid: [true, Validators.required],
		requiresApproval: [true, Validators.required]
	});

	constructor() {
		this.load();
	}

	private load(): void {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.settingsService.getLeaveTypes(companyId).subscribe(types => this.leaveTypes.set(types));
		}
	}

	startAdd(): void {
		this.editingId = null;
		this.error.set('');
		this.form.reset({ name: '', defaultEntitlementDays: 0, isPaid: true, requiresApproval: true });
		this.formMode.set('add');
	}

	startEdit(item: LeaveType): void {
		this.editingId = item.id;
		this.error.set('');
		this.form.reset({
			name: item.name,
			defaultEntitlementDays: item.defaultEntitlementDays,
			isPaid: item.isPaid,
			requiresApproval: item.requiresApproval
		});
		this.formMode.set('edit');
	}

	cancel(): void {
		this.formMode.set(null);
		this.editingId = null;
	}

	save(): void {
		if (this.form.invalid) return;
		const value = this.form.getRawValue();

		if (this.editingId) {
			const current = this.leaveTypes().find(t => t.id === this.editingId);
			this.settingsService.updateLeaveType(this.editingId, {
				name: value.name!,
				defaultEntitlementDays: value.defaultEntitlementDays!,
				isPaid: value.isPaid!,
				requiresApproval: value.requiresApproval!,
				isActive: current?.isActive ?? true
			}).subscribe({
				next: () => { this.formMode.set(null); this.editingId = null; this.load(); },
				error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to save leave type.')
			});
			return;
		}

		const companyId = this.auth.companyId();
		if (!companyId) return;
		this.settingsService.createLeaveType({
			companyId,
			name: value.name!,
			defaultEntitlementDays: value.defaultEntitlementDays!,
			isPaid: value.isPaid!,
			requiresApproval: value.requiresApproval!
		}).subscribe({
			next: () => { this.formMode.set(null); this.load(); },
			error: response => this.error.set(response.error?.errors?.[0] ?? 'Unable to create leave type.')
		});
	}

	toggleActive(item: LeaveType): void {
		this.settingsService.updateLeaveType(item.id, {
			name: item.name,
			defaultEntitlementDays: item.defaultEntitlementDays,
			isPaid: item.isPaid,
			requiresApproval: item.requiresApproval,
			isActive: !item.isActive
		}).subscribe(() => this.load());
	}
}

