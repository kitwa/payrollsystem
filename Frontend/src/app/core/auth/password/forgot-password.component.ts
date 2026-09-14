import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-forgot-password', standalone: true, imports: [CommonModule, ReactiveFormsModule, RouterLink],
  styleUrl: '../auth-shared.scss',
  template: `
    <div class="auth-page">
      <div class="auth-layout">
        <section class="auth-intro d-none d-lg-flex">
          <a class="auth-brand" routerLink="/" aria-label="Payroll SA home">
            <span class="auth-brand__mark">PS</span><span>Payroll SA</span>
          </a>
          <div class="auth-intro__content">
            <p class="auth-eyebrow">South African payroll, made clear</p>
            <h1>Run payroll with confidence.</h1>
            <p>Keep employees, statutory calculations, payslips, and payroll operations in one focused workspace.</p>
          </div>
          <small class="auth-intro__footer">Secure access for your payroll team.</small>
        </section>

        <section class="auth-panel">
          <div class="auth-panel__inner">
            <a class="auth-brand auth-brand--mobile d-lg-none" routerLink="/" aria-label="Payroll SA home">
              <span class="auth-brand__mark">PS</span><span>Payroll SA</span>
            </a>
            <p class="auth-eyebrow">Account recovery</p>
            <h2>Forgot your password?</h2>
            <p class="auth-muted">Enter your email address and, if an account exists, we will send reset instructions.</p>
            @if (message()) { <div class="alert alert-success">{{ message() }}</div> }
            @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
            <form [formGroup]="form" (ngSubmit)="submit()">
              <div class="mb-3">
                <label class="form-label" for="forgot-email">Email</label>
                <input id="forgot-email" class="form-control form-control-lg" type="email" formControlName="email" autocomplete="email">
              </div>
              <button class="btn btn-primary btn-lg w-100" type="submit" [disabled]="form.invalid || loading()">{{ loading() ? 'Sending...' : 'Send reset link' }}</button>
              <a routerLink="/login" class="btn btn-link w-100 mt-2">Back to sign in</a>
            </form>
          </div>
        </section>
      </div>
    </div>
  `
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder);
  readonly loading = signal(false); readonly message = signal(''); readonly error = signal('');
  readonly form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });
  submit(): void { if (this.form.invalid) return; this.loading.set(true); this.message.set(''); this.error.set(''); this.auth.forgotPassword(this.form.value.email!).subscribe({ next: () => { this.loading.set(false); this.message.set('If an account exists with that email, reset instructions have been sent.'); }, error: () => { this.loading.set(false); this.message.set('If an account exists with that email, reset instructions have been sent.'); } }); }
}
