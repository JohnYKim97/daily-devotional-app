import { Component, inject, signal, computed, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DatePipe } from '@angular/common';
import { Router } from '@angular/router';

import { JournalService } from '../../core/services/journal.service';
import { DateService } from '../../core/services/date.service';
import { NotificationService } from '../../core/services/notification.service';
import { JournalHistoryEntry } from '../../core/models/journal-history-entry.model';
import { BIBLE_BOOKS } from '../../core/constants/bible-books';

@Component({
  selector: 'app-history',
  imports: [DatePipe],
  templateUrl: './history.component.html',
  styleUrl: './history.component.scss',
})
export class HistoryComponent {
  private journalService = inject(JournalService);
  private dateService = inject(DateService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  protected readonly today = this.dateService.getToday();

  protected readonly journals = signal<JournalHistoryEntry[]>([]);
  protected readonly bookFilter = signal('');
  protected readonly dateFilter = signal('');
  protected readonly notesFilter = signal('');

  protected readonly availableBooks = computed(() => {
    const booksWithEntries = new Set(this.journals().map((entry) => entry.book));

    return BIBLE_BOOKS.filter((book) => booksWithEntries.has(book));
  });

  protected readonly groupedJournals = computed(() => {
    const book = this.bookFilter();
    const date = this.dateFilter();
    const notesQuery = this.notesFilter().trim().toLowerCase();

    const filtered = this.journals().filter(
      (entry) =>
        (!book || entry.book === book) &&
        (!date || entry.date === date) &&
        (!notesQuery || entry.notes.toLowerCase().includes(notesQuery)),
    );

    const entriesByBook = new Map<string, JournalHistoryEntry[]>();

    for (const entry of filtered) {
      const entries = entriesByBook.get(entry.book) ?? [];
      entries.push(entry);
      entriesByBook.set(entry.book, entries);
    }

    return BIBLE_BOOKS.filter((book) => entriesByBook.has(book)).map((book) => ({
      book,
      entries: entriesByBook.get(book)!,
    }));
  });

  private hasScrolledToToday = false;

  constructor() {
    this.journalService.getAllJournals().subscribe({
      next: (journals) => this.journals.set(journals),
      error: (error) => {
        console.error('Error loading journal history: ', error);
        this.notificationService.show('Could not load the reading schedule.', 'error');
      },
    });

    effect(() => {
      const groups = this.groupedJournals();

      if (!isPlatformBrowser(this.platformId) || this.hasScrolledToToday || groups.length === 0) {
        return;
      }

      queueMicrotask(() => {
        const element = document.getElementById('today-entry');

        if (element) {
          element.scrollIntoView({ behavior: 'smooth', block: 'center' });
          this.hasScrolledToToday = true;
        }
      });
    });
  }

  passageReference(entry: JournalHistoryEntry): string {
    const verses =
      entry.startVerse === entry.endVerse
        ? `${entry.startVerse}`
        : `${entry.startVerse}-${entry.endVerse}`;

    return `${entry.book} ${entry.chapter}:${verses}`;
  }

  onBookFilterChange(event: Event): void {
    this.bookFilter.set((event.target as HTMLSelectElement).value);
  }

  onDateFilterChange(event: Event): void {
    this.dateFilter.set((event.target as HTMLInputElement).value);
  }

  onNotesFilterChange(event: Event): void {
    this.notesFilter.set((event.target as HTMLInputElement).value);
  }

  clearFilters(): void {
    this.bookFilter.set('');
    this.dateFilter.set('');
    this.notesFilter.set('');
  }

  editEntry(date: string): void {
    this.dateService.setDate(date);
    this.router.navigateByUrl('/');
  }
}
