import { Injectable, signal } from '@angular/core';

import { Journal } from '../models/journal.model';
import { TODAY_JOURNAL } from '../../features/journal/journal.mock';

@Injectable({ providedIn: 'root' })
export class JournalService {
  private _journal = signal<Journal>({
    ...TODAY_JOURNAL,
  });

  readonly journal = this._journal.asReadonly();

  saveJournal(journal: Journal): void {
    this._journal.set({
      ...journal,
    });
  }
}
