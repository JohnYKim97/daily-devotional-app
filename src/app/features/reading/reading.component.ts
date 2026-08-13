import { Component, input, inject, OnInit, signal, effect } from '@angular/core';

import { Passage } from '../../core/models/passage.model';

import { PassageHeaderComponent } from './components/passage-header/passage-header.component';
import { CommentaryComponent } from './components/commentary/commentary.component';
import { VerseListComponent } from './components/verse-list/verse-list.component';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DailyReading } from '../../core/models/daily-reading.model';

import { DateService } from '../../core/services/date.service';

@Component({
  selector: 'app-reading',
  imports: [PassageHeaderComponent, CommentaryComponent, VerseListComponent],
  templateUrl: './reading.component.html',
  styleUrl: './reading.component.scss',
})
export class ReadingComponent {
  private readingService = inject(DailyReadingService);
  private dateService = inject(DateService);

  private _reading = signal<DailyReading | null>(null);
  readonly reading = this._reading.asReadonly();

  private _passage = signal<Passage | null>(null);
  readonly passage = this._passage.asReadonly();

  private _loading = signal(false);
  readonly loading = this._loading.asReadonly();

  private _error = signal(false);
  readonly error = this._error.asReadonly();

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();
      this.loadReading(date);
    });
  }

  loadReading(date: string): void {
    this._loading.set(true);
    this._error.set(false);
    this._passage.set(null);
    this._reading.set(null);

    this.readingService.getReadingByDate(date).subscribe({
      next: (reading) => {
        this._reading.set(reading);

        const passage: Passage = {
          book: reading.book,
          chapter: reading.chapter,
          startVerse: reading.startVerse,
          endVerse: reading.endVerse,
          date: reading.date,
          verses: reading.verses,
        };

        this._passage.set(passage);
        this._loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load reading: ', err);

        this._loading.set(false);
        this._error.set(true);
      },
    });
  }
}
