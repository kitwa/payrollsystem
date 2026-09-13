import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { SupportTicketService } from '../../../features/support/services/support-ticket.service';
import { SupportTicketType } from '../../../features/support/models/support-ticket.models';

@Component({
  selector: 'app-support-chatbot',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    @if (visible()) {
      <div class="support-chatbot">
        @if (open()) {
          <section class="support-chatbot__panel shadow-lg" role="dialog" aria-label="Support chat">
            <header class="support-chatbot__header">
              <span><i class="bi bi-headset me-2"></i>Support</span>
              <button class="btn btn-sm support-chatbot__close" type="button" (click)="close()" aria-label="Close support chat">
                <i class="bi bi-x-lg"></i>
              </button>
            </header>

            <div class="support-chatbot__body">
              @if (!submitted()) {
                <p class="text-muted small mb-3">Need help? Send us a message and our team will get back to you.</p>
                @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }
                <form [formGroup]="form" (ngSubmit)="submit()">
                  <div class="mb-2">
                    <label class="form-label small mb-1">Subject</label>
                    <input class="form-control form-control-sm" formControlName="subject" maxlength="200">
                  </div>
                  <div class="mb-2">
                    <label class="form-label small mb-1">Ticket type</label>
                    <select class="form-select form-select-sm" formControlName="type">
                      @for (type of types; track type.value) { <option [ngValue]="type.value">{{ type.label }}</option> }
                    </select>
                  </div>
                  <div class="mb-3">
                    <label class="form-label small mb-1">Description</label>
                    <textarea class="form-control form-control-sm" rows="4" formControlName="description" placeholder="Describe the issue or question"></textarea>
                  </div>
                  <button class="btn btn-primary btn-sm w-100" type="submit" [disabled]="form.invalid || submitting()">
                    {{ submitting() ? 'Sending…' : 'Send' }}
                  </button>
                </form>
              } @else {
                <div class="text-center py-3">
                  <i class="bi bi-check-circle-fill text-success fs-2 d-block mb-2"></i>
                  <p class="mb-3">Your support ticket was created and sent to our team.</p>
                  <button class="btn btn-outline-secondary btn-sm" type="button" (click)="reset()">Send another message</button>
                </div>
              }
            </div>
          </section>
        }

        <button class="support-chatbot__toggle shadow" type="button" (click)="toggle()" [attr.aria-expanded]="open()" aria-label="Open support chat">
          <i class="bi" [class.bi-chat-dots-fill]="!open()" [class.bi-x-lg]="open()"></i>
        </button>
      </div>
    }
  `,
  styles: [`
    .support-chatbot { position: fixed; z-index: 1075; right: 1rem; bottom: max(1rem, env(safe-area-inset-bottom)); display: flex; flex-direction: column; align-items: flex-end; gap: .75rem; }
    .support-chatbot__toggle { width: 3.25rem; height: 3.25rem; border-radius: 50%; border: 0; display: inline-grid; place-items: center; background: #0d6efd; color: #fff; font-size: 1.25rem; }
    .support-chatbot__toggle:hover { background: #0a58ca; }
    .support-chatbot__panel { width: min(22rem, calc(100vw - 2rem)); max-height: min(32rem, calc(100vh - 6rem)); display: flex; flex-direction: column; background: #fff; border-radius: .75rem; overflow: hidden; border: 1px solid #dee2e6; }
    .support-chatbot__header { display: flex; align-items: center; justify-content: space-between; padding: .75rem 1rem; background: #0d6efd; color: #fff; font-weight: 600; }
    .support-chatbot__close { color: #fff; border: 0; line-height: 1; }
    .support-chatbot__close:hover { background: rgba(255,255,255,.15); }
    .support-chatbot__body { padding: 1rem; overflow-y: auto; }

    @media (max-width: 575.98px) {
      .support-chatbot { right: .75rem; bottom: max(.75rem, env(safe-area-inset-bottom)); }
      .support-chatbot__panel { width: calc(100vw - 1.5rem); }
    }
  `]
})
export class SupportChatbotComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(SupportTicketService);

  readonly open = signal(false);
  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly error = signal('');

  readonly visible = () => this.auth.isLoggedIn() && !this.auth.isInRole('SuperAdmin');

  readonly types = [
    { value: SupportTicketType.TechnicalIssue, label: 'Technical Issue' },
    { value: SupportTicketType.BugReport, label: 'Bug Report' },
    { value: SupportTicketType.FeatureRequest, label: 'Feature Request' },
    { value: SupportTicketType.PayrollQuestion, label: 'Payroll Question' },
    { value: SupportTicketType.Other, label: 'Other' }
  ];

  readonly form = this.fb.group({
    subject: ['', [Validators.required, Validators.maxLength(200)]],
    type: [SupportTicketType.TechnicalIssue, Validators.required],
    description: ['', [Validators.required, Validators.maxLength(10000)]]
  });

  toggle(): void {
    this.open.update(value => !value);
  }

  close(): void {
    this.open.set(false);
  }

  reset(): void {
    this.submitted.set(false);
    this.error.set('');
    this.form.reset({ type: SupportTicketType.TechnicalIssue });
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) return;
    this.submitting.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    this.ticketService.create({
      subject: value.subject!.trim(),
      type: value.type!,
      description: value.description!.trim()
    }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: response => {
        this.submitting.set(false);
        this.error.set(response.error?.errors?.[0] ?? 'Unable to submit support ticket.');
      }
    });
  }
}
