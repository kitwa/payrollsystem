import { Injectable, signal } from '@angular/core';

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>;
}

@Injectable({ providedIn: 'root' })
export class InstallPromptService {
  private readonly dismissedKey = 'payrollsa_install_prompt_dismissed_until';
  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  readonly canInstall = signal(false);
  readonly installed = signal(this.isStandalone());

  constructor() {
    if (this.installed() || this.isDismissed()) return;
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
    const oneWeek = Date.now() + 7 * 24 * 60 * 60 * 1000;
    localStorage.setItem(this.dismissedKey, String(oneWeek));
    this.canInstall.set(false);
  }

  private isDismissed(): boolean {
    const dismissedUntil = Number(localStorage.getItem(this.dismissedKey) ?? 0);
    if (dismissedUntil > Date.now()) return true;
    localStorage.removeItem(this.dismissedKey);
    localStorage.removeItem('payrollsa_install_prompt_dismissed');
    return false;
  }

  private isStandalone(): boolean {
    return window.matchMedia('(display-mode: standalone)').matches
      || (window.navigator as Navigator & { standalone?: boolean }).standalone === true;
  }
}
