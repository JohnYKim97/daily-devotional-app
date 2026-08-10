import { Component } from '@angular/core';

import { Passage } from '../../core/models/passage.model';
import { TODAY_READING } from '../reading/reading.mock';

import { ReadingComponent } from '../reading/reading.component';
import { JournalComponent } from '../journal/journal.component';

@Component({
  selector: 'app-dashboard',
  imports: [ReadingComponent, JournalComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  passage: Passage = TODAY_READING;
}
