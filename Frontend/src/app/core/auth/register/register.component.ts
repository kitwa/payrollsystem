import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-vh-100 py-4 bg-light d-flex align-items-center">
      <div class="container" style="max-width: 760px;">
        <div class="card shadow-sm border-0">
          <div class="card-body p-4 p-md-5">
            <div class="d-flex justify-content-between align-items-start mb-4 gap-3">
              <div>
                <h4 class="fw-bold mb-1 text-primary">Register Your Company</h4>
                <p class="text-secondary mb-0">Create your Payroll SA workspace and admin account.</p>
              </div>
              <a routerLink="/login" class="btn btn-outline-secondary btn-sm">Sign In</a>
            </div>

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <h6 class="fw-semibold mb-3">Company Details</h6>
              <div class="row g-3 mb-4">
                <div class="col-12 col-md-6">
                  <label class="form-label">Company Name</label>
                  <input formControlName="companyName" type="text" class="form-control" placeholder="Acme (Pty) Ltd">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Registration Number</label>
                  <input formControlName="registrationNumber" type="text" class="form-control" placeholder="2024/123456/07">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Company Email (optional)</label>
                  <input formControlName="companyEmail" type="email" class="form-control" placeholder="payroll@acme.co.za">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Phone (optional)</label>
                  <input formControlName="phone" type="text" class="form-control" placeholder="+27 11 555 1234">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Tax Number (optional)</label>
                  <input formControlName="taxNumber" type="text" class="form-control" placeholder="9999/999/99/9">
                </div>
              </div>

              <h6 class="fw-semibold mb-3">Admin Account</h6>
              <div class="row g-3">
                <div class="col-12 col-md-6">
                  <label class="form-label">First Name</label>
                  <input formControlName="adminFirstName" type="text" class="form-control">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Last Name</label>
                  <input formControlName="adminLastName" type="text" class="form-control">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Admin Email</label>
                  <input formControlName="adminEmail" type="email" class="form-control" placeholder="admin@acme.co.za">
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">Password</label>
                  <input formControlName="password" type="password" class="form-control" placeholder="At least 8 chars, uppercase, number, symbol">
                </div>
              </div>

              @if (error()) {
                <div class="alert alert-danger py-2 mt-4 mb-0">{{ error() }}</div>
              }

              <button type="submit" class="btn btn-primary w-100 mt-4" [disabled]="loading() || form.invalid">
                @if (loading()) { <span class="spinner-border spinner-border-sm me-2"></span> }
                Create Company and Continue
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  loading = signal(false);
  error = signal('');

  form: ReturnType<FormBuilder['group']>;

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      companyName: ['', [Validators.required, Validators.maxLength(200)]],
      registrationNumber: ['', [Validators.required, Validators.maxLength(100)]],
      companyEmail: ['', [Validators.email]],
      phone: [''],
      taxNumber: [''],
      adminFirstName: ['', [Validators.required, Validators.maxLength(100)]],
      adminLastName: ['', [Validators.required, Validators.maxLength(100)]],
      adminEmail: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set('');

    const payload = {
      companyName: this.form.value.companyName!.trim(),
      registrationNumber: this.form.value.registrationNumber!.trim(),
      companyEmail: this.cleanOptional(this.form.value.companyEmail),
      phone: this.cleanOptional(this.form.value.phone),
      taxNumber: this.cleanOptional(this.form.value.taxNumber),
      adminFirstName: this.form.value.adminFirstName!.trim(),
      adminLastName: this.form.value.adminLastName!.trim(),
      adminEmail: this.form.value.adminEmail!.trim(),
      password: this.form.value.password!
    };

    this.auth.registerCompany(payload).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: err => {
        const apiErrors = err?.error?.errors;
        this.error.set(Array.isArray(apiErrors) ? apiErrors.join(' ') : 'Could not register company.');
        this.loading.set(false);
      }
    });
  }

  private cleanOptional(value: string | null | undefined) {
    const trimmed = value?.trim();
    return trimmed ? trimmed : null;
  }
}
