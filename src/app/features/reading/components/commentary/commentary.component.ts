import { Component, input } from '@angular/core';

@Component({
  selector: 'app-commentary',
  imports: [],
  templateUrl: './commentary.component.html',
  styleUrl: './commentary.component.scss',
})
export class CommentaryComponent {
  commentary = input<string>('');
}
