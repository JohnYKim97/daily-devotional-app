import { Component, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Verse } from '../../core/models/verse.model';
import { Passage } from '../../core/models/passage.model';
import { Journal } from './../../core/models/journal.model';

import { JournalService } from '../../core/services/journal.service';

@Component({
  selector: 'app-journal',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './journal.component.html',
  styleUrl: './journal.component.scss',
})
export class JournalComponent {
  private journalService = inject(JournalService);
  passage = input.required<Passage>();
  journal = this.journalService.journal;
  selectedVerseNumber?: number;
  saved = false;
  notes = '';

  get selectedVerse(): Verse | undefined {
    return this.passage().verses.find((verse) => verse.number === this.selectedVerseNumber);
  }

  constructor() {
    this.selectedVerseNumber = this.journal().favoriteVerse;
    this.notes = this.journal().notes;
  }

  saveJournal(): void {
    const journal: Journal = {
      ...this.journal(),
      favoriteVerse: this.selectedVerseNumber,
      notes: this.notes,
    };

    this.journalService.saveJournal(journal).subscribe({
      next: (savedJournal) => {
        this.journalService.setJournal(savedJournal);
        this.saved = true;
        console.log('Journal Saved:', savedJournal);
      },
      error: (error) => {
        console.error('Error saving journal:', error);
      },
    });
  }
}
