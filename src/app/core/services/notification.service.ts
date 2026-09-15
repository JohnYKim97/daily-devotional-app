import { Service, signal, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

import { Notification } from '../models/notification.model';

const DISPLAY_DURATION_MS = 5000;

@Service()
export class NotificationService {
  private platformId = inject(PLATFORM_ID);

  private _notification = signal<Notification | null>(null);
  readonly notification = this._notification.asReadonly();

  private dismissTimeoutId?: ReturnType<typeof setTimeout>;

  show(message: string, type: Notification['type'] = 'success'): void {
    this._notification.set({ message, type });

    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    clearTimeout(this.dismissTimeoutId);
    this.dismissTimeoutId = setTimeout(() => this._notification.set(null), DISPLAY_DURATION_MS);
  }

  dismiss(): void {
    this._notification.set(null);
  }
}
