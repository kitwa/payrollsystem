import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-reset-password', standalone: true, imports: [CommonModule, ReactiveFormsModule, RouterLink],
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
            <h2>Reset password</h2>
            <p class="auth-muted">Choose a new password for your Payroll SA account.</p>
            @if (message()) { <div class="alert alert-success">{{ message() }} <a routerLink="/login">Sign in</a></div> }
            @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
            <form [formGroup]="form" (ngSubmit)="submit()">
              <div class="mb-3">
                <label class="form-label" for="reset-password">New password</label>
                <div class="input-group">
                  <input id="reset-password" class="form-control form-control-lg" [type]="showPassword() ? 'text' : 'password'" formControlName="password" autocomplete="new-password">
                  <button class="btn btn-outline-secondary" type="button" (click)="showPassword.set(!showPassword())"><i class="bi" [class.bi-eye]="!showPassword()" [class.bi-eye-slash]="showPassword()"></i></button>
                </div>
              </div>
              <div class="mb-3">
                <label class="form-label" for="reset-confirm">Confirm password</label>
                <div class="input-group">
                  <input id="reset-confirm" class="form-control form-control-lg" [type]="showConfirm() ? 'text' : 'password'" formControlName="confirm" autocomplete="new-password">
                  <button class="btn btn-outline-secondary" type="button" (click)="showConfirm.set(!showConfirm())"><i class="bi" [class.bi-eye]="!showConfirm()" [class.bi-eye-slash]="showConfirm()"></i></button>
                </div>
              </div>
              <button class="btn btn-primary btn-lg w-100" type="submit" [disabled]="form.invalid || loading()">{{ loading() ? 'Resetting...' : 'Reset password' }}</button>
            </form>
          </div>
        </section>
      </div>
    </div>
  `
})
export class ResetPasswordComponent {
  private readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder); private readonly route = inject(ActivatedRoute); private readonly router = inject(Router);
  readonly loading = signal(false); readonly message = signal(''); readonly error = signal('');
  readonly showPassword = signal(false); readonly showConfirm = signal(false);
  readonly form = this.fb.group({ password: ['', [Validators.required, Validators.minLength(8)]], confirm: ['', Validators.required] });
  submit(): void { const email = this.route.snapshot.queryParamMap.get('email'); const token = this.route.snapshot.queryParamMap.get('token'); const value = this.form.getRawValue(); if (this.form.invalid || value.password !== value.confirm || !email || !token) { this.error.set('Check the password fields and reset link.'); return; } this.loading.set(true); this.auth.resetPassword(email, token, value.password!).subscribe({ next: () => { this.loading.set(false); this.message.set('Password reset successfully.'); }, error: response => { this.loading.set(false); this.error.set(response.error?.errors?.[0] ?? 'The reset link is invalid or expired.'); } }); }
}
