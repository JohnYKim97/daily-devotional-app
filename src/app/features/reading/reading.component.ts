import { Component, inject, effect } from '@angular/core';

import { PassageHeaderComponent } from './components/passage-header/passage-header.component';
import { CommentaryComponent } from './components/commentary/commentary.component';
import { VerseListComponent } from './components/verse-list/verse-list.component';

import { DailyReading } from '../../core/models/daily-reading.model';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DateService } from '../../core/services/date.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';

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

  readonly reading = this.readingStateService.reading;
  readonly loading = this.readingStateService.loading;
  readonly error = this.readingStateService.error;

  constructor() {
    effect(() => {
      const date = this.dateService.selectedDate();
      this.readingStateService.loadReading(date);
    });
  }
}
