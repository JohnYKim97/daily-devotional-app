import { Component, inject, effect, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

import { Verse } from '../../core/models/verse.model';
import { Journal } from './../../core/models/journal.model';

import { JournalService } from '../../core/services/journal.service';
import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DailyReading } from '../../core/models/daily-reading.model';

import { DateService } from '../../core/services/date.service';

@Component({
  selector: 'app-journal',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './journal.component.html',
  styleUrl: './journal.component.scss',
})
export class JournalComponent {
  private journalService = inject(JournalService);
  private dailyReadingService = inject(DailyReadingService);
  private dateService = inject(DateService);

  private _reading = signal<DailyReading | null>(null);
  readonly reading = this._reading.asReadonly();

  journal = this.journalService.journal;
  selectedVerseNumber: number | null = null;
  saved = false;
  notes = '';

  get selectedVerse(): Verse | undefined {
    return this.reading()?.verses.find((verse) => verse.number === this.selectedVerseNumber);
  }

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();

      this.journalService.loadJournalForDate(date);
      this.loadReading(date);
    });

    effect(() => {
      const journal = this.journal();

      this.selectedVerseNumber = journal.favoriteVerse ?? null;
      this.notes = journal.notes;
    });
  }

  saveJournal(): void {
    const journal: Journal = {
      ...this.journal(),
      favoriteVerse: this.selectedVerseNumber ?? null,
      notes: this.notes,
    };

    const request$ = journal.id
      ? this.journalService.updateJournal(journal.date, journal)
      : this.journalService.saveJournal(journal);

    request$.subscribe({
      next: (savedJournal) => {
        this.journalService.setJournal(savedJournal);
        this.saved = true;

        console.log('Journal saved: ', savedJournal);
      },
      error: (error) => {
        console.error('Error saving journal ', error);
      },
    });
  }

  private loadReading(date: string): void {
    this._reading.set(null);

    this.dailyReadingService.getReadingByDate(date).subscribe({
      next: (reading) => {
        this._reading.set(reading);
      },
      error: (error) => {
        console.error('Error loading daily readings: ', error);
      },
    });
  }
}
