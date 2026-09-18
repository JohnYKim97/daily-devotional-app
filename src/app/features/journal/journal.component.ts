import { Component, inject, effect, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
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
  private platformId = inject(PLATFORM_ID);

  readonly reading = this.readingStateService.reading;

  journal = this.journalService.journal;
  selectedVerseNumber: number | null = null;
  saved = signal(false);
  notes = '';

  protected readonly isExpanded = signal(false);
  protected readonly sheetHeightPx = signal(0);
  protected readonly isDragging = signal(false);

  private collapsedHeight = 0;
  private expandedHeight = 0;
  private dragStartY = 0;
  private dragStartHeight = 0;

  private savedMessageTimeout?: ReturnType<typeof setTimeout>;

  get selectedVerse(): Verse | undefined {
    return this.reading()?.verses.find((verse) => verse.number === this.selectedVerseNumber);
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

      this.selectedVerseNumber = journal.favoriteVerse ?? null;
      this.notes = journal.notes;
    });

    if (isPlatformBrowser(this.platformId)) {
      this.setupSheetHeights();
    }
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
