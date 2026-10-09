import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
  untracked,
} from '@angular/core';

import { PassageHeaderComponent } from './components/passage-header/passage-header.component';
import { CommentaryComponent } from './components/commentary/commentary.component';
import { VerseListComponent } from './components/verse-list/verse-list.component';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DateService } from '../../core/services/date.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';
import { NotificationService } from '../../core/services/notification.service';
import { TranslationService } from '../../core/services/translation.service';
import { DailyReading } from '../../core/models/daily-reading.model';
import { Verse } from '../../core/models/verse.model';
import { PassageRange } from '../../core/models/passage-range.model';
import { formatPassageReference } from '../../core/utils/passage-reference';

interface ScrollAnchor {
  verse: string;
  offset: number;
}

@Component({
  selector: 'app-reading',
  imports: [PassageHeaderComponent, CommentaryComponent, VerseListComponent],
  templateUrl: './reading.component.html',
  styleUrl: './reading.component.scss',
})
export class ReadingComponent {
  private readingService = inject(DailyReadingService);
  private dateService = inject(DateService);
  private readingStateService = inject(DailyReadingStateService);
  private notificationService = inject(NotificationService);
  protected translationService = inject(TranslationService);
  private host = inject<ElementRef<HTMLElement>>(ElementRef);
  private injector = inject(Injector);

  readonly reading = this.readingStateService.reading;
  readonly loading = this.readingStateService.loading;
  readonly error = this.readingStateService.error;
  readonly rateLimited = this.readingStateService.rateLimited;

  readonly commentaryExpanded = signal(false);
  readonly commentaryLoading = signal(false);

  // Verses of the whole chapter(s), once "Read full chapter" has been used. Cleared
  // whenever a different reading is loaded.
  protected readonly fullChapterVerses = linkedSignal<unknown, Verse[] | null>({
    source: this.reading,
    computation: () => null,
  });
  protected readonly fullChapterLoading = signal(false);

  // Whether the reader is in "full chapter" mode. It outlives a change of Bible version
  // (the chapter is loaded again in the new version) but ends when another day is opened.
  private readonly fullChapterWanted = signal(false);
  private lastDate: string | null = null;
  private scrollAnchor: ScrollAnchor | null = null;

  protected readonly displayedVerses = computed(
    () => this.fullChapterVerses() ?? this.reading()?.verses ?? [],
  );

  // The daily passage, marked inside the full chapter. Null while only the daily passage shows.
  protected readonly dailyPassage = computed<PassageRange | null>(() => {
    const reading = this.reading();

    if (!reading || !this.fullChapterVerses()) {
      return null;
    }

    return {
      chapter: reading.chapter,
      startVerse: reading.startVerse,
      endChapter: Math.max(reading.chapter, reading.endChapter),
      endVerse: reading.endVerse,
    };
  });

  // E.g. "Jeremiah 10:11-25", shown on the highlighted verses inside the full chapter.
  protected readonly dailyPassageReference = computed(() => {
    const reading = this.reading();

    return reading ? formatPassageReference(reading) : '';
  });

  protected readonly canReadFullChapter = computed(() => {
    const reading = this.reading();

    return !!reading && !reading.coversWholeChapters && !!reading.translationCode;
  });

  protected readonly spansChapters = computed(() => {
    const reading = this.reading();

    return !!reading && reading.endChapter > reading.chapter;
  });

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();

      if (!this.translationService.loaded()) {
        return;
      }

      if (date !== this.lastDate) {
        this.lastDate = date;
        this.fullChapterWanted.set(false);
      }

      this.readingStateService.loadReading(date, this.translationService.selectedCode());
    });

    // A reading that arrives while full chapter mode is on (e.g. after switching version)
    // gets its whole chapter loaded straight away.
    effect(() => {
      const reading = this.reading();

      if (reading) {
        untracked(() => {
          if (this.fullChapterWanted() && this.canReadFullChapter()) {
            this.loadFullChapter(reading);
          }
        });
      }
    });

    // After switching version, puts the reader back on the verse they were looking at
    // once the new text (and the full chapter, if open) is on screen.
    effect(() => {
      const ready =
        !this.loading() &&
        !!this.reading() &&
        !this.fullChapterLoading() &&
        (!untracked(() => this.fullChapterWanted()) || !!this.fullChapterVerses());

      if (this.error() || ready) {
        untracked(() => this.restoreScrollAnchor());
      }
    });
  }

  // Asks for the current day's reading again, e.g. after a rate limit.
  retry(): void {
    this.readingStateService.loadReading(
      this.dateService.selectedDate(),
      this.translationService.selectedCode(),
    );
  }

  onVersionChange(event: Event): void {
    this.scrollAnchor = this.captureScrollAnchor();
    this.translationService.select(Number((event.target as HTMLSelectElement).value));
  }

  // The verse at the top of the screen, and how far below the top of the scrolling area it
  // sits. Verses are found by number because their text (and so their height) differs
  // between versions, so a plain scroll position would land somewhere else.
  private captureScrollAnchor(): ScrollAnchor | null {
    const area = this.scrollArea();
    const top = area.top;

    for (const verse of this.host.nativeElement.querySelectorAll<HTMLElement>('.verse')) {
      const rect = verse.getBoundingClientRect();

      if (rect.bottom > top) {
        return { verse: verse.dataset['verse'] ?? '', offset: rect.top - top };
      }
    }

    return null;
  }

  private restoreScrollAnchor(): void {
    const anchor = this.scrollAnchor;

    if (!anchor) {
      return;
    }

    afterNextRender(
      () => {
        this.scrollAnchor = null;

        const verse = this.host.nativeElement.querySelector<HTMLElement>(
          `.verse[data-verse="${anchor.verse}"]`,
        );

        if (!verse) {
          return;
        }

        const area = this.scrollArea();
        const shift = verse.getBoundingClientRect().top - area.top - anchor.offset;

        if (area.element) {
          area.element.scrollTop += shift;
        } else {
          window.scrollBy({ top: shift });
        }
      },
      { injector: this.injector },
    );
  }

  // Whatever scrolls the verses: the verse card on wide screens, the page on mobile.
  private scrollArea(): { element: HTMLElement | null; top: number } {
    const card = this.host.nativeElement.querySelector<HTMLElement>('.verse-card');

    if (card && card.scrollHeight > card.clientHeight) {
      return { element: card, top: card.getBoundingClientRect().top };
    }

    return { element: null, top: 0 };
  }

  onToggleFullChapter(): void {
    const reading = this.reading();

    if (!reading || this.fullChapterLoading()) {
      return;
    }

    if (this.fullChapterVerses()) {
      this.fullChapterWanted.set(false);
      this.fullChapterVerses.set(null);
      this.scrollToTop();
      return;
    }

    this.fullChapterWanted.set(true);
    this.loadFullChapter(reading);
  }

  private loadFullChapter(reading: DailyReading): void {
    this.fullChapterLoading.set(true);

    this.readingService
      .getFullChapters(
        reading.translationCode,
        reading.bookId,
        reading.chapter,
        Math.max(reading.chapter, reading.endChapter),
      )
      .subscribe({
        next: (passage) => {
          this.fullChapterLoading.set(false);

          // Ignore a late response if the user already moved to another day or version.
          if (this.reading() !== reading) {
            return;
          }

          this.fullChapterVerses.set(passage.verses);

          // After a version switch the reader's place is restored instead.
          if (!this.scrollAnchor) {
            this.scrollToDailyPassage();
          }
        },
        error: (error) => {
          console.error('Error loading full chapter: ', error);
          this.fullChapterLoading.set(false);
          this.fullChapterWanted.set(false);
          this.notificationService.show(
            error?.error?.errors?.[0] ?? 'Could not load the full chapter. Please try again.',
            'error',
          );
        },
      });
  }

  // Back to the daily passage: start reading it from the top, on both the verse card
  // (which scrolls on wide screens) and the page (which scrolls on mobile).
  private scrollToTop(): void {
    afterNextRender(
      () => {
        const verseCard = this.host.nativeElement.querySelector('.verse-card');

        if (verseCard) {
          verseCard.scrollTop = 0;
        }

        window.scrollTo({ top: 0 });
      },
      { injector: this.injector },
    );
  }

  private scrollToDailyPassage(): void {
    afterNextRender(
      () => {
        this.host.nativeElement
          .querySelector('.verse-group-highlighted')
          ?.scrollIntoView({ block: 'start' });
      },
      { injector: this.injector },
    );
  }

  onToggleCommentary(): void {
    const expanding = !this.commentaryExpanded();
    this.commentaryExpanded.set(expanding);

    const currentReading = this.reading();

    if (expanding && currentReading && !currentReading.commentary && !this.commentaryLoading()) {
      this.commentaryLoading.set(true);

      this.readingService.generateCommentary(currentReading.date).subscribe({
        next: ({ commentary }) => {
          this.readingStateService.updateCommentary(commentary);
          this.commentaryLoading.set(false);
        },
        error: (error) => {
          console.error('Error generating commentary: ', error);
          this.notificationService.show(
            'Could not generate commentary. Please try again.',
            'error',
          );
          this.commentaryLoading.set(false);
        },
      });
    }
  }
}
