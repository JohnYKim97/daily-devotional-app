import { Component, input, output } from '@angular/core';

import { DailyReading } from '../../../../core/models/daily-reading.model';

@Component({
  selector: 'app-passage-header',
  imports: [],
  templateUrl: './passage-header.component.html',
  styleUrl: './passage-header.component.scss',
})
export class PassageHeaderComponent {
  reading = input.required<DailyReading>();
  commentaryExpanded = input<boolean>(false);
  toggleCommentary = output<void>();
}
