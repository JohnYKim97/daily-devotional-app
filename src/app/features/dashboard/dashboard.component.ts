import { Component, inject } from '@angular/core';

import { ReadingComponent } from '../reading/reading.component';
import { JournalComponent } from '../journal/journal.component';
import { DateNavComponent } from '../../shared/date-nav/date-nav.component';
import { DateService } from '../../core/services/date.service';

@Component({
  selector: 'app-dashboard',
  imports: [ReadingComponent, JournalComponent, DateNavComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  protected dateService = inject(DateService);
}
