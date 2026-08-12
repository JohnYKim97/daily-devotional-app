import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Journal } from '../models/journal.model';
import { TODAY_JOURNAL } from '../../features/journal/journal.mock';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class JournalService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:5432/api/journal';

  private _journal = signal<Journal>({
    ...TODAY_JOURNAL,
  });

  readonly journal = this._journal.asReadonly();

  getJournalByDate(date: string): Observable<Journal> {
    return this.http.get<Journal>(`${this.apiUrl}/${date}`);
  }

  saveJournal(journal: Journal): Observable<Journal> {
    return this.http.post<Journal>(this.apiUrl, journal);
  }

  setJournal(journal: Journal): void {
    this._journal.set({
      ...journal,
    });
  }
}
