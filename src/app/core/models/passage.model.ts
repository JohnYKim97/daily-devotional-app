import { Verse } from './verse.model';

// The verses of a passage as returned by /api/translations/{code}/passage.
export interface Passage {
  translationCode: string;
  copyrightNotice: string;
  bookId: number;
  book: string;
  reference: string;
  coversWholeChapters: boolean;
  verses: Verse[];
}
