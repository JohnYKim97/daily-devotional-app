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
} from '@angular/core';

import { PassageHeaderComponent } from './components/passage-header/passage-header.component';
import { CommentaryComponent } from './components/commentary/commentary.component';
import { VerseListComponent } from './components/verse-list/verse-list.component';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DateService } from '../../core/services/date.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';
import { NotificationService } from '../../core/services/notification.service';
import { TranslationService } from '../../core/services/translation.service';
import { Verse } from '../../core/models/verse.model';
import { PassageRange } from '../../core/models/passage-range.model';
import { formatPassageReference } from '../../core/utils/passage-reference';

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
  private translationService = inject(TranslationService);
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

      this.readingStateService.loadReading(date, this.translationService.selectedCode());
    });
  }

  // Asks for the current day's reading again, e.g. after a rate limit.
  retry(): void {
    this.readingStateService.loadReading(
      this.dateService.selectedDate(),
      this.translationService.selectedCode(),
    );
  }

  onToggleFullChapter(): void {
    const reading = this.reading();

    if (!reading || this.fullChapterLoading()) {
      return;
    }

    if (this.fullChapterVerses()) {
      this.fullChapterVerses.set(null);
      this.scrollToTop();
      return;
    }

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

          // Ignore a late response if the user already moved to another day.
          if (this.reading()?.id !== reading.id) {
            return;
          }

          this.fullChapterVerses.set(passage.verses);
          this.scrollToDailyPassage();
        },
        error: (error) => {
          console.error('Error loading full chapter: ', error);
          this.fullChapterLoading.set(false);
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
