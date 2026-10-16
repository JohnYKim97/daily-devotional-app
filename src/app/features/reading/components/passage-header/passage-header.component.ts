import { Component, inject, computed, input, output } from '@angular/core';

import { Verse } from '../../../../core/models/verse.model';
import { DailyReading } from '../../../../core/models/daily-reading.model';
import { formatPassageReference } from '../../../../core/utils/passage-reference';
import { SettingsService } from '../../../../core/services/settings.service';
import { DateNavComponent } from '../../../../shared/date-nav/date-nav.component';

@Component({
  selector: 'app-passage-header',
  imports: [DateNavComponent],
  templateUrl: './passage-header.component.html',
  styleUrl: './passage-header.component.scss',
})
export class PassageHeaderComponent {
  protected settingsService = inject(SettingsService);

  reading = input.required<DailyReading>();
  commentaryExpanded = input<boolean>(false);
  // The verses of the whole chapter(s) while they are shown instead of just the daily passage.
  fullChapterVerses = input<Verse[] | null>(null);
  toggleCommentary = output<void>();

  protected readonly passageReference = computed(() => {
    const reading = this.reading();
    const verses = this.fullChapterVerses();

    if (!verses?.length) {
      return formatPassageReference(reading);
    }

    // E.g. "Jeremiah 13:1-27" for the whole chapter, even when today is only 13:15-27.
    const first = verses[0];
    const last = verses[verses.length - 1];

    return formatPassageReference({
      ...reading,
      chapter: first.chapter,
      startVerse: first.number,
      endChapter: last.chapter,
      endVerse: last.number,
    });
  });
}
