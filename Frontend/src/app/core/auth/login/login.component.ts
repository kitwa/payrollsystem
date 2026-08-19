import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div class="card shadow-sm" style="width: 400px">
        <div class="card-body p-4">
          <h4 class="card-title text-center mb-4 fw-bold text-primary">Payroll SA</h4>
          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="mb-3">
              <label class="form-label">Email</label>
              <input formControlName="email" type="email" class="form-control" placeholder="admin@payrollsa.co.za">
            </div>
            <div class="mb-3">
              <label class="form-label">Password</label>
              <input formControlName="password" type="password" class="form-control">
            </div>
            @if (error()) {
              <div class="alert alert-danger py-2">{{ error() }}</div>
            }
            <button type="submit" class="btn btn-primary w-100" [disabled]="loading()">
              @if (loading()) { <span class="spinner-border spinner-border-sm me-2"></span> }
              Sign In
            </button>
            <a routerLink="/register" class="btn btn-link w-100 mt-2">Register a new company</a>
          </form>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  loading = signal(false);
  error = signal('');
  form: ReturnType<FormBuilder['group']>;

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');
    const { email, password } = this.form.value;
    this.auth.login(email!, password!).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: () => { this.error.set('Invalid email or password.'); this.loading.set(false); }
    });
  }
}
