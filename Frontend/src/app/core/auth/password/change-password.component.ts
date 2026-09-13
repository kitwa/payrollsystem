import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-change-password', standalone: true, imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `<main class="container py-4"><section class="card border-0 shadow-sm" style="max-width:34rem"><div class="card-body"><h1 class="h3">Change password</h1><p class="text-muted">Update your password for this account.</p>@if (message()) { <div class="alert alert-success">{{ message() }}</div> } @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }<form [formGroup]="form" (ngSubmit)="submit()"><label class="form-label">Current password</label><div class="input-group mb-3"><input class="form-control" [type]="showCurrent() ? 'text' : 'password'" formControlName="currentPassword" autocomplete="current-password"><button class="btn btn-outline-secondary" type="button" (click)="showCurrent.set(!showCurrent())"><i class="bi" [class.bi-eye]="!showCurrent()" [class.bi-eye-slash]="showCurrent()"></i></button></div><label class="form-label">New password</label><div class="input-group mb-3"><input class="form-control" [type]="showNew() ? 'text' : 'password'" formControlName="newPassword" autocomplete="new-password"><button class="btn btn-outline-secondary" type="button" (click)="showNew.set(!showNew())"><i class="bi" [class.bi-eye]="!showNew()" [class.bi-eye-slash]="showNew()"></i></button></div><label class="form-label">Confirm new password</label><div class="input-group mb-3"><input class="form-control" [type]="showConfirm() ? 'text' : 'password'" formControlName="confirmPassword" autocomplete="new-password"><button class="btn btn-outline-secondary" type="button" (click)="showConfirm.set(!showConfirm())"><i class="bi" [class.bi-eye]="!showConfirm()" [class.bi-eye-slash]="showConfirm()"></i></button></div><button class="btn btn-dark" type="submit" [disabled]="form.invalid || loading()">{{ loading() ? 'Saving...' : 'Change password' }}</button><a routerLink="/profile" class="btn btn-link">Cancel</a></form></div></section></main>`
})
export class ChangePasswordComponent {
  private readonly auth = inject(AuthService); private readonly fb = inject(FormBuilder);
  readonly loading = signal(false); readonly message = signal(''); readonly error = signal('');
  readonly showCurrent = signal(false); readonly showNew = signal(false); readonly showConfirm = signal(false);
  readonly form = this.fb.group({ currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(8)]], confirmPassword: ['', Validators.required] });
  submit(): void { const value = this.form.getRawValue(); if (this.form.invalid || value.newPassword !== value.confirmPassword) { this.error.set('Check the password fields.'); return; } this.loading.set(true); this.error.set(''); this.auth.changePassword(value.currentPassword!, value.newPassword!).subscribe({ next: () => { this.loading.set(false); this.message.set('Password changed successfully.'); this.form.reset(); }, error: response => { this.loading.set(false); this.error.set(response.error?.errors?.[0] ?? 'Unable to change password.'); } }); }
}
