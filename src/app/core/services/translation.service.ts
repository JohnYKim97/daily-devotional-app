import { Service, signal, computed, inject, linkedSignal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Translation } from '../models/translation.model';
import { SettingsService } from './settings.service';
import { environment } from '../../../environments/environment';

@Service()
export class TranslationService {
  private http = inject(HttpClient);
  private settingsService = inject(SettingsService);
  private apiUrl = `${environment.apiUrl}/translations`;

  private _translations = signal<Translation[]>([]);
  readonly translations = this._translations.asReadonly();

  // True once the list has been requested, whether or not it succeeded, so the
  // reading page never waits forever if the translations request fails.
  private _loaded = signal(false);
  readonly loaded = this._loaded.asReadonly();

  // The translation being read right now. It starts as the preferred translation from
  // Settings, can be switched on the reading page, and falls back to the preferred one
  // again whenever that setting changes.
  private currentId = linkedSignal(() => this.settingsService.preferredTranslationId());

  // Code of the translation being read; undefined lets the server use its default.
  readonly selectedCode = computed(
    () => this._translations().find((t) => t.id === this.currentId())?.code,
  );

  // Switches the translation for this visit only; Settings holds the preferred one.
  select(id: number): void {
    this.currentId.set(id);
  }

  constructor() {
    this.http.get<Translation[]>(this.apiUrl).subscribe({
      next: (translations) => {
        this._translations.set(translations);
        this._loaded.set(true);
      },
      error: (error) => {
        console.error('Error loading translations: ', error);
        this._loaded.set(true);
      },
    });
  }
}
