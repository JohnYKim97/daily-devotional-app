import { Component, input } from '@angular/core';

import { Passage } from '../../core/models/passage.model';

import { PassageHeaderComponent } from './components/passage-header/passage-header.component';
import { CommentaryComponent } from './components/commentary/commentary.component';
import { VerseListComponent } from './components/verse-list/verse-list.component';

@Component({
  selector: 'app-reading',
  imports: [PassageHeaderComponent, CommentaryComponent, VerseListComponent],
  templateUrl: './reading.component.html',
  styleUrl: './reading.component.scss',
})
export class ReadingComponent {
  passage = input.required<Passage>();
}
