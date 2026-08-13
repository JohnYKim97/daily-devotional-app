import { Component, Input, input } from '@angular/core';
import { Passage } from '../../../../core/models/passage.model';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-passage-header',
  imports: [DatePipe],
  templateUrl: './passage-header.component.html',
  styleUrl: './passage-header.component.scss',
})
export class PassageHeaderComponent {
  passage = input.required<Passage>();
}
