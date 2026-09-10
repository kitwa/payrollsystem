import { Injectable, signal } from '@angular/core';

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>;
}

@Injectable({ providedIn: 'root' })
export class InstallPromptService {
  private readonly dismissedKey = 'payrollsa_install_prompt_dismissed';
  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  readonly canInstall = signal(false);
  readonly installed = signal(this.isStandalone());

  constructor() {
    if (this.installed() || localStorage.getItem(this.dismissedKey) === 'true') return;
    window.addEventListener('beforeinstallprompt', event => {
      event.preventDefault();
      this.deferredPrompt = event as BeforeInstallPromptEvent;
      this.canInstall.set(true);
    });
    window.addEventListener('appinstalled', () => {
      this.deferredPrompt = null;
      this.installed.set(true);
      this.canInstall.set(false);
    });
  }

  async install(): Promise<void> {
    if (!this.deferredPrompt) return;
    await this.deferredPrompt.prompt();
    const choice = await this.deferredPrompt.userChoice;
    if (choice.outcome === 'dismissed') this.dismiss();
    this.deferredPrompt = null;
    this.canInstall.set(false);
  }

  dismiss(): void {
    localStorage.setItem(this.dismissedKey, 'true');
    this.canInstall.set(false);
  }

  private isStandalone(): boolean {
    return window.matchMedia('(display-mode: standalone)').matches
      || (window.navigator as Navigator & { standalone?: boolean }).standalone === true;
  }
}
