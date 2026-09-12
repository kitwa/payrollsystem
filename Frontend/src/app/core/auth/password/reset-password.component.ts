import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-reset-password', standalone: true, imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `<main class="auth-page"><section class="auth-panel"><div class="auth-panel__inner"><p class="auth-eyebrow">Account recovery</p><h1>Reset password</h1><p class="auth-muted">Choose a new password for your Payroll SA account.</p>@if (message()) { <div class="alert alert-success">{{ message() }} <a routerLink="/login">Sign in</a></div> } @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }<form [formGroup]="form" (ngSubmit)="submit()"><label class="form-label" for="reset-password">New password</label><input id="reset-password" class="form-control form-control-lg mb-3" type="password" formControlName="password" autocomplete="new-password"><label class="form-label" for="reset-confirm">Confirm password</label><input id="reset-confirm" class="form-control form-control-lg mb-3" type="password" formControlName="confirm" autocomplete="new-password"><button class="btn btn-primary btn-lg w-100" type="submit" [disabled]="form.invalid || loading()">{{ loading() ? 'Resetting...' : 'Reset password' }}</button></form></div></section></main>`
})
export class ResetPasswordComponent {
  private readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder); private readonly route = inject(ActivatedRoute); private readonly router = inject(Router);
  readonly loading = signal(false); readonly message = signal(''); readonly error = signal('');
  readonly form = this.fb.group({ password: ['', [Validators.required, Validators.minLength(8)]], confirm: ['', Validators.required] });
  submit(): void { const email = this.route.snapshot.queryParamMap.get('email'); const token = this.route.snapshot.queryParamMap.get('token'); const value = this.form.getRawValue(); if (this.form.invalid || value.password !== value.confirm || !email || !token) { this.error.set('Check the password fields and reset link.'); return; } this.loading.set(true); this.auth.resetPassword(email, token, value.password!).subscribe({ next: () => { this.loading.set(false); this.message.set('Password reset successfully.'); }, error: response => { this.loading.set(false); this.error.set(response.error?.errors?.[0] ?? 'The reset link is invalid or expired.'); } }); }
}
