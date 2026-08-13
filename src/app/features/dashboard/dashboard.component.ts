import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';

import { ReadingComponent } from '../reading/reading.component';
import { JournalComponent } from '../journal/journal.component';

import { DateService } from '../../core/services/date.service';

@Component({
  selector: 'app-dashboard',
  imports: [ReadingComponent, JournalComponent, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  readonly dateService = inject(DateService);
  readonly selectedDate = this.dateService.selectedDate;
}
