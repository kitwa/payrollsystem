import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-forgot-password', standalone: true, imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `<main class="auth-page"><section class="auth-panel"><div class="auth-panel__inner"><p class="auth-eyebrow">Account recovery</p><h1>Forgot your password?</h1><p class="auth-muted">Enter your email address and, if an account exists, we will send reset instructions.</p>@if (message()) { <div class="alert alert-success">{{ message() }}</div> } @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }<form [formGroup]="form" (ngSubmit)="submit()"><label class="form-label" for="forgot-email">Email</label><input id="forgot-email" class="form-control form-control-lg mb-3" type="email" formControlName="email" autocomplete="email"><button class="btn btn-primary btn-lg w-100" type="submit" [disabled]="form.invalid || loading()">{{ loading() ? 'Sending...' : 'Send reset link' }}</button><a routerLink="/login" class="btn btn-link w-100 mt-2">Back to sign in</a></form></div></section></main>`
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder);
  readonly loading = signal(false); readonly message = signal(''); readonly error = signal('');
  readonly form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });
  submit(): void { if (this.form.invalid) return; this.loading.set(true); this.message.set(''); this.error.set(''); this.auth.forgotPassword(this.form.value.email!).subscribe({ next: () => { this.loading.set(false); this.message.set('If an account exists with that email, reset instructions have been sent.'); }, error: () => { this.loading.set(false); this.message.set('If an account exists with that email, reset instructions have been sent.'); } }); }
}
