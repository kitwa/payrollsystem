import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  loading = signal(false);
  error = signal('');
  showPassword = signal(false);

  form: ReturnType<FormBuilder['group']>;

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      companyName: ['', [Validators.required, Validators.maxLength(200)]],
      registrationNumber: ['', [Validators.required, Validators.maxLength(100)]],
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
