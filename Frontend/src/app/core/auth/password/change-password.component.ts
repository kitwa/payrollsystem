import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-change-password', standalone: true, imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `<main class="container py-4"><section class="card border-0 shadow-sm" style="max-width:34rem"><div class="card-body"><h1 class="h3">Change password</h1><p class="text-muted">Update your password for this account.</p>@if (message()) { <div class="alert alert-success">{{ message() }}</div> } @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }<form [formGroup]="form" (ngSubmit)="submit()"><label class="form-label">Current password</label><input class="form-control mb-3" type="password" formControlName="currentPassword" autocomplete="current-password"><label class="form-label">New password</label><input class="form-control mb-3" type="password" formControlName="newPassword" autocomplete="new-password"><label class="form-label">Confirm new password</label><input class="form-control mb-3" type="password" formControlName="confirmPassword" autocomplete="new-password"><button class="btn btn-dark" type="submit" [disabled]="form.invalid || loading()">{{ loading() ? 'Saving...' : 'Change password' }}</button><a routerLink="/dashboard" class="btn btn-link">Cancel</a></form></div></section></main>`
})
export class ChangePasswordComponent {
  private readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder);
  readonly loading = signal(false); readonly message = signal(''); readonly error = signal('');
  readonly form = this.fb.group({ currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(8)]], confirmPassword: ['', Validators.required] });
  submit(): void { const value = this.form.getRawValue(); if (this.form.invalid || value.newPassword !== value.confirmPassword) { this.error.set('Check the password fields.'); return; } this.loading.set(true); this.error.set(''); this.auth.changePassword(value.currentPassword!, value.newPassword!).subscribe({ next: () => { this.loading.set(false); this.message.set('Password changed successfully.'); this.form.reset(); }, error: response => { this.loading.set(false); this.error.set(response.error?.errors?.[0] ?? 'Unable to change password.'); } }); }
}
