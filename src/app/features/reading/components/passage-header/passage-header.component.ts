import { Component, Input, input } from '@angular/core';
import { Passage } from '../../../../core/models/passage.model';

@Component({
  selector: 'app-passage-header',
  imports: [],
  templateUrl: './passage-header.component.html',
  styleUrl: './passage-header.component.scss',
})
export class PassageHeaderComponent {
  passage = input.required<Passage>();
}
