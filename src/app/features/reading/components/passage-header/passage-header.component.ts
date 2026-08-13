import { Component, input } from '@angular/core';

import { DailyReading } from '../../../../core/models/daily-reading.model';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-passage-header',
  imports: [DatePipe],
  templateUrl: './passage-header.component.html',
  styleUrl: './passage-header.component.scss',
})
export class PassageHeaderComponent {
  reading = input.required<DailyReading>();
}
