import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { SettingsService } from '../../services/settings.service';

@Component({
	selector: 'app-general-settings',
	standalone: true,
	imports: [CommonModule, ReactiveFormsModule],
	template: `
		<section class="mb-3">
			<h1 class="h3 mb-1">General Settings</h1>
			<p class="text-muted mb-0">Configure company profile and payroll defaults.</p>
		</section>

		<section class="row g-3">
			<div class="col-12 col-xl-7">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Company Profile</h2>
						<form class="row g-2" [formGroup]="companyForm" (ngSubmit)="save()">
							<div class="col-12 col-md-6"><label class="form-label">Company Name</label><input class="form-control" formControlName="name"></div>
							<div class="col-12 col-md-6"><label class="form-label">Registration Number</label><input class="form-control" formControlName="registrationNumber"></div>
							<div class="col-12 col-md-6"><label class="form-label">Tax Number</label><input class="form-control" formControlName="taxNumber"></div>
							<div class="col-12 col-md-6"><label class="form-label">Contact Email</label><input class="form-control" formControlName="email"></div>
							<div class="col-12"><button class="btn btn-dark" type="submit" [disabled]="companyForm.invalid">Save Settings</button></div>
						</form>
						@if (saved()) {
							<div class="alert alert-success mt-3 mb-0">Company profile saved.</div>
						}
					</div>
				</article>
			</div>
		</section>
	`
})
export class GeneralSettingsComponent {
	private readonly fb = inject(FormBuilder);
	private readonly auth = inject(AuthService);
	private readonly settingsService = inject(SettingsService);

	readonly saved = signal(false);

	readonly companyForm = this.fb.group({
		name: ['', Validators.required],
		registrationNumber: ['', Validators.required],
		taxNumber: [''],
		email: ['', Validators.email]
	});

	constructor() {
		const companyId = this.auth.companyId();
		if (companyId) {
			this.settingsService.getCompany(companyId).subscribe(company => {
				this.companyForm.patchValue({
					name: company.name,
					registrationNumber: company.registrationNumber,
					taxNumber: company.taxNumber,
					email: company.email
				});
			});
		}
	}

	save(): void {
		const companyId = this.auth.companyId();
		if (this.companyForm.invalid || !companyId) {
			return;
		}
		const value = this.companyForm.getRawValue();
		this.settingsService.updateCompany(companyId, {
			name: value.name!,
			registrationNumber: value.registrationNumber!,
			taxNumber: value.taxNumber ?? undefined,
			email: value.email ?? undefined
		}).subscribe(() => this.saved.set(true));
	}
}

