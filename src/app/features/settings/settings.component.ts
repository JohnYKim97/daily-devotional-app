import { Component, inject } from '@angular/core';

import { SettingsService } from '../../core/services/settings.service';
import { ThemePreference } from '../../core/models/user-settings.model';

@Component({
  selector: 'app-settings',
  imports: [],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  protected settingsService = inject(SettingsService);

  protected readonly themeOptions: { value: ThemePreference; label: string }[] = [
    { value: 'system', label: 'System' },
    { value: 'light', label: 'Light' },
    { value: 'dark', label: 'Dark' },
  ];

  toggleShowFavoriteVerse(): void {
    this.settingsService.setShowFavoriteVersesInNotes(
      !this.settingsService.showFavoriteVersesInNotes(),
    );
  }
}
