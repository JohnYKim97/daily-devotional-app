import { Service, signal, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';

import { UserSettings, ThemePreference } from '../models/user-settings.model';
import { NotificationService } from './notification.service';
import { AuthService } from './auth.service';

const DEFAULT_SETTINGS: UserSettings = {
  showFavoriteVerseInNotes: false,
  theme: 'system',
};

@Service()
export class SettingsService {
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private platformId = inject(PLATFORM_ID);
  private apiUrl = 'http://localhost:5184/api/usersettings';

  private _settings = signal<UserSettings>(DEFAULT_SETTINGS);
  readonly showFavoriteVerseInNotes = computed(() => this._settings().showFavoriteVerseInNotes);
  readonly theme = computed(() => this._settings().theme);

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    effect(() => {
      if (this.authService.isAuthenticated()) {
        this.loadSettings();
      } else {
        this._settings.set(DEFAULT_SETTINGS);
      }
    });

    effect(() => {
      const theme = this.theme();

      if (theme === 'system') {
        delete document.documentElement.dataset['theme'];
      } else {
        document.documentElement.dataset['theme'] = theme;
      }
    });
  }

  setShowFavoriteVersesInNotes(show: boolean): void {
    this.updateSettings({ ...this._settings(), showFavoriteVerseInNotes: show });
  }

  setTheme(theme: ThemePreference): void {
    this.updateSettings({ ...this._settings(), theme });
  }

  private loadSettings(): void {
    this.http.get<UserSettings>(this.apiUrl).subscribe({
      next: (settings) => {
        this._settings.set(settings);
      },
      error: (error) => {
        console.error('Error loading settings: ', error);
      },
    });
  }

  private updateSettings(settings: UserSettings): void {
    this._settings.set(settings);

    this.http.put<UserSettings>(this.apiUrl, settings).subscribe({
      next: (saved) => {
        this._settings.set(saved);
      },
      error: (error) => {
        console.error('Error updating settings: ', error);
        this.notificationService.show('Could not save your settings. Please try again.', 'error');
      },
    });
  }
}
