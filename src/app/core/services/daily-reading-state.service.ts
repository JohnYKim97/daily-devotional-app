import { Injectable, inject, signal } from '@angular/core';

import { DailyReading } from '../models/daily-reading.model';
import { DailyReadingService } from '../services/daily-reading.service';

@Injectable({
  providedIn: 'root',
})
export class DailyReadingStateService {
  private dailyReadingService = inject(DailyReadingService);

  private _reading = signal<DailyReading | null>(null);
  readonly reading = this._reading.asReadonly();

  private _loading = signal(false);
  readonly loading = this._loading.asReadonly();
  private _error = signal(false);
  readonly error = this._error.asReadonly();
  // True when the API refused the request because of too many requests.
  private _rateLimited = signal(false);
  readonly rateLimited = this._rateLimited.asReadonly();

  loadReading(date: string, translationCode?: string): void {
    this._loading.set(true);
    this._error.set(false);
    this._rateLimited.set(false);
    this._reading.set(null);

    this.dailyReadingService.getReadingByDate(date, translationCode).subscribe({
      next: (reading) => {
        this._reading.set(reading);
        this._loading.set(false);
      },
      error: (err) => {
        console.error('Error loading daily reading: ', err);
        this._reading.set(null);
        this._loading.set(false);
        this._error.set(true);
        this._rateLimited.set(err.status === 429);
      },
    });
  }

  updateCommentary(commentary: string): void {
    const current = this._reading();

    if (current) {
      this._reading.set({ ...current, commentary });
    }
  }
}
