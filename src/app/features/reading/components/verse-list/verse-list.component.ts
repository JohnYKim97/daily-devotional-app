import { Component, computed, input, signal } from '@angular/core';

import { Verse } from '../../../../core/models/verse.model';
import { PassageRange } from '../../../../core/models/passage-range.model';

interface TextPart {
  text: string;
  italic: boolean;
}

interface VerseSegment {
  text: string;
  // Set when this segment is a footnote marker rather than verse text.
  footnoteKey: string | null;
  footnoteLabel: string;
}

interface DisplayFootnote {
  key: string;
  label: string;
  parts: TextPart[];
}

interface DisplayVerse {
  key: string;
  number: number;
  headingTitle: string | null;
  headingSubtitles: string[];
  segments: VerseSegment[];
  footnotes: DisplayFootnote[];
}

// Consecutive verses that are all inside (or all outside) the highlighted range.
interface VerseGroup {
  key: string;
  highlighted: boolean;
  verses: DisplayVerse[];
}

// Footnotes are lettered (a, b, ... z, aa, ab, ...) so they can't be mistaken for verse numbers.
function footnoteLetters(position: number): string {
  let letters = '';

  for (let n = position; n > 0; n = Math.floor((n - 1) / 26)) {
    letters = String.fromCharCode(97 + ((n - 1) % 26)) + letters;
  }

  return letters;
}

const FOOTNOTE_MARKER = /\[\[(\d+)\]\]/g;

@Component({
  selector: 'app-verse-list',
  templateUrl: './verse-list.component.html',
  styleUrl: './verse-list.component.scss',
})
export class VerseListComponent {
  verses = input.required<Verse[]>();
  // When set, verses inside this range are marked as the daily passage.
  highlight = input<PassageRange | null>(null);
  // Shown after "Today's passage ·" on the highlighted verses, e.g. "Jeremiah 10:11-25".
  highlightReference = input<string>('');

  protected readonly highlightLabel = computed(() =>
    this.highlightReference()
      ? `Today’s passage · ${this.highlightReference()}`
      : 'Today’s passage',
  );

  private openFootnotes = signal<ReadonlySet<string>>(new Set());

  protected readonly groups = computed(() => {
    const range = this.highlight();
    const groups: VerseGroup[] = [];
    let position = 0;

    for (const verse of this.verses()) {
      const key = `${verse.chapter}-${verse.number}`;
      const footnotes: DisplayFootnote[] = [];
      const segments: VerseSegment[] = [];
      let last = 0;

      for (const match of verse.text.matchAll(FOOTNOTE_MARKER)) {
        const body = verse.footnotes?.[Number(match[1]) - 1];

        if (body === undefined) {
          continue;
        }

        position++;
        const label = footnoteLetters(position);
        const footnoteKey = `${key}-${position}`;

        segments.push({
          text: verse.text.slice(last, match.index),
          footnoteKey: null,
          footnoteLabel: '',
        });
        segments.push({ text: '', footnoteKey, footnoteLabel: label });
        footnotes.push({ key: footnoteKey, label, parts: this.toParts(body) });
        last = match.index + match[0].length;
      }

      segments.push({ text: verse.text.slice(last), footnoteKey: null, footnoteLabel: '' });

      const [headingTitle, ...headingSubtitles] = (verse.heading ?? '')
        .split('\n')
        .filter((line) => line.trim() !== '');

      const displayVerse: DisplayVerse = {
        key,
        number: verse.number,
        headingTitle: headingTitle ?? null,
        headingSubtitles,
        segments,
        footnotes,
      };

      const highlighted = range !== null && this.isInRange(verse, range);
      const current = groups[groups.length - 1];

      if (current && current.highlighted === highlighted) {
        current.verses.push(displayVerse);
      } else {
        groups.push({ key, highlighted, verses: [displayVerse] });
      }
    }

    return groups;
  });

  protected isOpen(footnoteKey: string): boolean {
    return this.openFootnotes().has(footnoteKey);
  }

  protected toggleFootnote(footnoteKey: string): void {
    this.openFootnotes.update((open) => {
      const next = new Set(open);

      if (!next.delete(footnoteKey)) {
        next.add(footnoteKey);
      }

      return next;
    });
  }

  private isInRange(verse: Verse, range: PassageRange): boolean {
    const afterStart =
      verse.chapter > range.chapter ||
      (verse.chapter === range.chapter && verse.number >= range.startVerse);
    const beforeEnd =
      verse.chapter < range.endChapter ||
      (verse.chapter === range.endChapter && verse.number <= range.endVerse);

    return afterStart && beforeEnd;
  }

  // Footnote bodies mark italics with *asterisks*.
  private toParts(body: string): TextPart[] {
    return body
      .split('*')
      .map((text, index) => ({ text, italic: index % 2 === 1 }))
      .filter((part) => part.text !== '');
  }
}
