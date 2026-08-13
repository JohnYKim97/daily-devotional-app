import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Journal } from '../models/journal.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class JournalService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5184/api/journal';

  private _journal = signal<Journal>({
    id: 0,
    date: '',
    favoriteVerse: null,
    notes: '',
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

  updateJournal(date: string, journal: Journal): Observable<Journal> {
    return this.http.put<Journal>(`${this.apiUrl}/${date}`, journal);
  }

  loadJournalForDate(date: string): void {
    this.getJournalByDate(date).subscribe({
      next: (journal) => {
        this.setJournal(journal);
      },
      error: (err) => {
        if (err.status === 404) {
          const emptyJournal: Journal = {
            id: 0,
            date: date,
            favoriteVerse: null,
            notes: '',
          };

          this.setJournal(emptyJournal);

          return;
        }
        console.error('Error loading journal: ', err);
      },
    });
  }
}
