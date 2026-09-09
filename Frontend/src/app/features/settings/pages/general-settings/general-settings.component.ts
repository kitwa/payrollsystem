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

			<div class="col-12 col-xl-5">
				<article class="card border-0 shadow-sm h-100">
					<div class="card-body">
						<h2 class="h5 mb-3">Company Logo</h2>
						<p class="text-muted small mb-3">Shown on generated payslips. PNG, JPEG, or WEBP up to 2MB.</p>

						<div class="logo-preview mb-3">
							@if (hasLogo()) {
								<img [src]="logoUrl()" alt="Company logo" class="logo-preview__image">
							} @else {
								<span class="text-muted">No logo uploaded yet.</span>
							}
						</div>

						<div class="d-flex gap-2 flex-wrap">
							<label class="btn btn-outline-dark btn-sm mb-0">
								Choose Image
								<input type="file" accept="image/png,image/jpeg,image/webp" class="d-none" (change)="onLogoSelected($event)">
							</label>
							@if (hasLogo()) {
								<button class="btn btn-outline-danger btn-sm" type="button" (click)="removeLogo()">Remove Logo</button>
							}
						</div>

						@if (logoMessage()) {
							<div class="alert mt-3 mb-0" [class.alert-success]="logoMessageTone() === 'success'" [class.alert-danger]="logoMessageTone() === 'error'">
								{{ logoMessage() }}
							</div>
						}
					</div>
				</article>
			</div>
		</section>
	`,
	styles: [
		`
			.logo-preview {
				display: flex;
				align-items: center;
				justify-content: center;
				height: 8rem;
				border: 1px dashed #d4d4d8;
				border-radius: 0.6rem;
				background: #fafafa;
			}

			.logo-preview__image {
				max-height: 100%;
				max-width: 100%;
				object-fit: contain;
			}
		`
	]
})
export class GeneralSettingsComponent {
	private readonly fb = inject(FormBuilder);
	private readonly auth = inject(AuthService);
	private readonly settingsService = inject(SettingsService);

	readonly saved = signal(false);
	readonly hasLogo = signal(false);
	readonly logoCacheBust = signal(Date.now());
	readonly logoMessage = signal('');
	readonly logoMessageTone = signal<'success' | 'error'>('success');

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
				this.hasLogo.set(company.hasLogo);
			});
		}
	}

	logoUrl(): string {
		const companyId = this.auth.companyId();
		return companyId ? `${this.settingsService.logoUrl(companyId)}?v=${this.logoCacheBust()}` : '';
	}

	onLogoSelected(event: Event): void {
		const companyId = this.auth.companyId();
		const file = (event.target as HTMLInputElement).files?.[0];
		if (!companyId || !file) return;

		this.logoMessage.set('');
		this.settingsService.uploadLogo(companyId, file).subscribe({
			next: () => {
				this.hasLogo.set(true);
				this.logoCacheBust.set(Date.now());
				this.logoMessageTone.set('success');
				this.logoMessage.set('Company logo uploaded successfully.');
			},
			error: response => {
				this.logoMessageTone.set('error');
				this.logoMessage.set(response.error?.errors?.[0] ?? 'Unable to upload the logo.');
			}
		});
	}

	removeLogo(): void {
		const companyId = this.auth.companyId();
		if (!companyId) return;

		this.logoMessage.set('');
		this.settingsService.removeLogo(companyId).subscribe({
			next: () => {
				this.hasLogo.set(false);
				this.logoMessageTone.set('success');
				this.logoMessage.set('Company logo removed.');
			},
			error: response => {
				this.logoMessageTone.set('error');
				this.logoMessage.set(response.error?.errors?.[0] ?? 'Unable to remove the logo.');
			}
		});
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

