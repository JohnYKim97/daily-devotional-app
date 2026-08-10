import { Component, Input, input } from '@angular/core';
import { Verse } from '../../../../core/models/verse.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verse-list',
  imports: [CommonModule],
  templateUrl: './verse-list.component.html',
  styleUrl: './verse-list.component.scss',
})
export class VerseListComponent {
  verses = input.required<Verse[]>();
}
