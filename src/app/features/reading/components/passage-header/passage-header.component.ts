import { Component, inject, computed, input, output } from '@angular/core';

import { DailyReading } from '../../../../core/models/daily-reading.model';
import { formatPassageReference } from '../../../../core/utils/passage-reference';
import { SettingsService } from '../../../../core/services/settings.service';

@Component({
  selector: 'app-passage-header',
  imports: [],
  templateUrl: './passage-header.component.html',
  styleUrl: './passage-header.component.scss',
})
export class PassageHeaderComponent {
  protected settingsService = inject(SettingsService);

  reading = input.required<DailyReading>();
  commentaryExpanded = input<boolean>(false);
  toggleCommentary = output<void>();

  protected readonly passageReference = computed(() => formatPassageReference(this.reading()));
}
