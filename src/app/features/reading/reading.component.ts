import { Component, inject, effect, signal } from '@angular/core';

import { PassageHeaderComponent } from './components/passage-header/passage-header.component';
import { CommentaryComponent } from './components/commentary/commentary.component';
import { VerseListComponent } from './components/verse-list/verse-list.component';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DateService } from '../../core/services/date.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';
import { NotificationService } from '../../core/services/notification.service';

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

  readonly reading = this.readingStateService.reading;
  readonly loading = this.readingStateService.loading;
  readonly error = this.readingStateService.error;

  readonly commentaryExpanded = signal(false);
  readonly commentaryLoading = signal(false);

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();
      this.readingStateService.loadReading(date);
    });
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
