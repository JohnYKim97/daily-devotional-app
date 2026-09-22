import { Component, inject, effect, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { from } from 'rxjs';

import { Verse } from '../../core/models/verse.model';
import { Journal } from './../../core/models/journal.model';

import { JournalService } from '../../core/services/journal.service';
import { DateService } from '../../core/services/date.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';
import { NotificationService } from '../../core/services/notification.service';
import { SettingsService } from '../../core/services/settings.service';
import { formatPassageReference } from '../../core/utils/passage-reference';

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
  protected settingsService = inject(SettingsService);
  private platformId = inject(PLATFORM_ID);

  readonly reading = this.readingStateService.reading;

  journal = this.journalService.journal;
  selectedVerseNumber = signal<number | null>(null);
  saved = signal(false);
  notes = signal('');

  protected readonly isExpanded = signal(false);
  protected readonly sheetHeightPx = signal(0);
  protected readonly isDragging = signal(false);

  private collapsedHeight = 0;
  private expandedHeight = 0;
  private dragStartY = 0;
  private dragStartHeight = 0;

  private savedMessageTimeout?: ReturnType<typeof setTimeout>;

  get selectedVerse(): Verse | undefined {
    return this.reading()?.verses.find((verse) => verse.number === this.selectedVerseNumber());
  }

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();

      if (!isPlatformBrowser(this.platformId)) {
        return;
      }

      this.journalService.loadJournalForDate(date);
    });

    effect(() => {
      const journal = this.journal();

      this.selectedVerseNumber.set(journal.favoriteVerse ?? null);
      this.notes.set(journal.notes);
    });

    effect(() => {
      const height = this.sheetHeightPx();

      if (!isPlatformBrowser(this.platformId)) {
        return;
      }

      document.documentElement.style.setProperty('--sheet-height', `${height}px`);
    });

    if (isPlatformBrowser(this.platformId)) {
      this.setupSheetHeights();
    }
  }

  saveJournal(): void {
    const journal: Journal = {
      ...this.journal(),
      favoriteVerse: this.selectedVerseNumber() ?? null,
      notes: this.notes(),
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

  shareJournal(): void {
    const reading = this.reading();

    if (!reading) {
      return;
    }

    const dateLabel = new Date(`${this.dateService.selectedDate()}T00:00:00`).toLocaleDateString(
      'en-US',
      { month: 'long', day: 'numeric', year: 'numeric' },
    );
    const passageLabel = formatPassageReference(reading);
    const text = `${dateLabel}\n${passageLabel}\n\n${this.notes}`;

    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    if (typeof navigator.share === 'function') {
      from(navigator.share({ text })).subscribe({
        error: (error) => {
          if ((error as DOMException).name !== 'AbortError') {
            console.error('Error sharing journal: ', error);
          }
        },
      });
      return;
    }

    from(navigator.clipboard.writeText(text)).subscribe({
      next: () => this.notificationService.show('Copied to clipboard!', 'success'),
      error: (error) => {
        console.error('Error copying journal: ', error);
        this.notificationService.show('Could not copy to clipboard.', 'error');
      },
    });
  }

  toggleSheet(): void {
    this.isExpanded.update((expanded) => !expanded);
    this.sheetHeightPx.set(this.isExpanded() ? this.expandedHeight : this.collapsedHeight);
  }

  onDragStart(event: PointerEvent): void {
    this.isDragging.set(true);
    this.dragStartY = event.clientY;
    this.dragStartHeight = this.sheetHeightPx();

    (event.target as HTMLElement).setPointerCapture(event.pointerId);
  }

  onDragMove(event: PointerEvent): void {
    if (!this.isDragging()) {
      return;
    }

    const delta = this.dragStartY - event.clientY;
    const newHeight = this.dragStartHeight + delta;

    this.sheetHeightPx.set(
      Math.min(this.expandedHeight, Math.max(this.collapsedHeight, newHeight)),
    );
  }

  onDragEnd(): void {
    if (!this.isDragging()) {
      return;
    }

    this.isDragging.set(false);

    const midpoint = (this.collapsedHeight + this.expandedHeight) / 2;
    const snapToExpanded = this.sheetHeightPx() > midpoint;

    this.isExpanded.set(snapToExpanded);
    this.sheetHeightPx.set(snapToExpanded ? this.expandedHeight : this.collapsedHeight);
  }

  private setupSheetHeights(): void {
    const availableHeight = window.innerHeight - 64 - 70;

    this.collapsedHeight = availableHeight * 0.4;
    this.expandedHeight = availableHeight * 0.85;
    this.sheetHeightPx.set(this.collapsedHeight);
  }

  private showSavedMessage(): void {
    this.saved.set(true);

    clearTimeout(this.savedMessageTimeout);
    this.savedMessageTimeout = setTimeout(() => this.saved.set(false), 3000);
  }
}
