import { Component, inject, effect, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Verse } from '../../core/models/verse.model';
import { Journal } from './../../core/models/journal.model';

import { JournalService } from '../../core/services/journal.service';
import { DateService } from '../../core/services/date.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-journal',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './journal.component.html',
  styleUrl: './journal.component.scss',
})
export class JournalComponent {
  private journalService = inject(JournalService);
  private dateService = inject(DateService);
  private readingStateService = inject(DailyReadingStateService);
  private notificationService = inject(NotificationService);

  readonly reading = this.readingStateService.reading;

  journal = this.journalService.journal;
  selectedVerseNumber: number | null = null;
  saved = signal(false);
  notes = '';

  private savedMessageTimeout?: ReturnType<typeof setTimeout>;

  get selectedVerse(): Verse | undefined {
    return this.reading()?.verses.find((verse) => verse.number === this.selectedVerseNumber);
  }

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();

      this.journalService.loadJournalForDate(date);
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
        this.showSavedMessage();

        console.log('Journal saved: ', savedJournal);
      },
      error: (error) => {
        console.error('Error saving journal ', error);
        this.notificationService.show(
          'Could not save your journal entry. Please try again.',
          'error',
        );
      },
    });
  }

  private showSavedMessage(): void {
    this.saved.set(true);

    clearTimeout(this.savedMessageTimeout);
    this.savedMessageTimeout = setTimeout(() => this.saved.set(false), 3000);
  }
}
