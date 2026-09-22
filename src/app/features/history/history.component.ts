import { Component, inject, signal, computed, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DatePipe } from '@angular/common';
import { Router } from '@angular/router';

import { JournalService } from '../../core/services/journal.service';
import { DateService } from '../../core/services/date.service';
import { NotificationService } from '../../core/services/notification.service';
import { JournalHistoryEntry } from '../../core/models/journal-history-entry.model';
import { BIBLE_BOOKS } from '../../core/constants/bible-books';
import { formatPassageReference } from '../../core/utils/passage-reference';

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
  protected readonly authorFilter = signal('');
  protected readonly notesFilter = signal('');
  protected readonly expandedEntryIds = signal<Set<number>>(new Set());

  protected readonly availableBooks = computed(() => {
    const booksWithEntries = new Set(this.journals().map((entry) => entry.book));

    return BIBLE_BOOKS.filter((book) => booksWithEntries.has(book));
  });

  protected readonly authorDisplayNames = computed(() => {
    const byUserId = new Map<string, { name: string; isOwn: boolean }>();

    for (const entry of this.journals()) {
      if (!byUserId.has(entry.authorUserId)) {
        byUserId.set(entry.authorUserId, { name: entry.authorName, isOwn: entry.isOwnEntry });
      }
    }

    const nameCounts = new Map<string, number>();
    for (const author of byUserId.values()) {
      nameCounts.set(author.name, (nameCounts.get(author.name) ?? 0) + 1);
    }

    const displayNames = new Map<string, string>();

    for (const [userId, author] of byUserId) {
      const hasCollision = (nameCounts.get(author.name) ?? 0) > 1;
      displayNames.set(userId, hasCollision && author.isOwn ? 'You' : author.name);
    }

    return displayNames;
  });

  protected readonly availableAuthors = computed(() => {
    const displayNames = this.authorDisplayNames();

    return [...displayNames.entries()]
      .map(([userId, displayName]) => ({ userId, displayName }))
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  });
  protected readonly groupedJournals = computed(() => {
    const book = this.bookFilter();
    const date = this.dateFilter();
    const author = this.authorFilter();
    const notesQuery = this.notesFilter().trim().toLowerCase();

    const filtered = this.journals().filter(
      (entry) =>
        entry.date <= this.today &&
        (!book || entry.book === book) &&
        (!date || entry.date === date) &&
        (!author || entry.authorUserId === author) &&
        (!notesQuery || entry.notes.toLowerCase().includes(notesQuery)),
    );

    const entriesByDate = new Map<string, JournalHistoryEntry[]>();

    for (const entry of filtered) {
      const entries = entriesByDate.get(entry.date) ?? [];
      entries.push(entry);
      entriesByDate.set(entry.date, entries);
    }

    return [...entriesByDate.entries()]
      .sort(([dateA], [dateB]) => (dateA < dateB ? 1 : -1))
      .map(([date, entries]) => ({ date, entries }));
  });

  private hasScrolledToToday = false;

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.journalService.getAllJournals().subscribe({
        next: (journals) => this.journals.set(journals),
        error: (error) => {
          console.error('Error loading journal history: ', error);
          this.notificationService.show('Could not load your journal history.', 'error');
        },
      });
    }

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
    return formatPassageReference(entry);
  }

  authorDisplayName(entry: JournalHistoryEntry): string {
    return this.authorDisplayNames().get(entry.authorUserId) ?? entry.authorName;
  }

  isExpanded(entryId: number): boolean {
    return this.expandedEntryIds().has(entryId);
  }

  isLongNote(notes: string): boolean {
    return notes.length > 140;
  }

  toggleExpand(entryId: number): void {
    this.expandedEntryIds.update((ids) => {
      const next = new Set(ids);

      if (next.has(entryId)) {
        next.delete(entryId);
      } else {
        next.add(entryId);
      }

      return next;
    });
  }

  onBookFilterChange(event: Event): void {
    this.bookFilter.set((event.target as HTMLSelectElement).value);
  }

  onDateFilterChange(event: Event): void {
    this.dateFilter.set((event.target as HTMLInputElement).value);
  }

  onAuthorFilterChange(event: Event): void {
    this.authorFilter.set((event.target as HTMLInputElement).value);
  }

  onNotesFilterChange(event: Event): void {
    this.notesFilter.set((event.target as HTMLInputElement).value);
  }

  clearFilters(): void {
    this.bookFilter.set('');
    this.dateFilter.set('');
    this.authorFilter.set('');
    this.notesFilter.set('');
  }

  editEntry(date: string): void {
    this.dateService.setDate(date);
    this.router.navigateByUrl('/');
  }
}
