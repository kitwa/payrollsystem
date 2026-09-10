import { Component, inject } from '@angular/core';
import { InstallPromptService } from '../../../core/pwa/install-prompt.service';

@Component({
  selector: 'app-install-prompt',
  standalone: true,
  template: `
    @if (prompt.canInstall() && !prompt.installed()) {
      <aside class="install-prompt shadow-lg" role="dialog" aria-label="Install Payroll SA">
        <div><strong class="d-block">Install Payroll SA</strong><small class="text-muted">Faster access and a better mobile experience.</small></div>
        <div class="d-flex gap-2 flex-shrink-0"><button class="btn btn-sm btn-dark" type="button" (click)="prompt.install()">Install</button><button class="btn btn-sm btn-link text-secondary" type="button" (click)="prompt.dismiss()">Later</button></div>
      </aside>
    }
  `,
  styles: [`
    .install-prompt { position: fixed; z-index: 1080; right: 1rem; bottom: max(1rem, env(safe-area-inset-bottom)); max-width: min(24rem, calc(100vw - 2rem)); display: flex; align-items: center; gap: 1rem; padding: .9rem 1rem; background: #fff; border: 1px solid #dee2e6; border-radius: .75rem; }
  `]
})
export class InstallPromptComponent {
  readonly prompt = inject(InstallPromptService);
}
