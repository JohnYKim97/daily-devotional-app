import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';

import { DateService } from '../../core/services/date.service';

@Component({
  selector: 'app-date-nav',
  imports: [DatePipe],
  templateUrl: './date-nav.component.html',
  styleUrl: './date-nav.component.scss',
})
export class DateNavComponent {
  protected dateService = inject(DateService);
}
