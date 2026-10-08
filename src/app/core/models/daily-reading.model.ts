import { Verse } from '../models/verse.model';

export interface DailyReading {
  id: number;
  date: string;
  bookId: number;
  book: string;
  chapter: number;
  endChapter: number;
  startVerse: number;
  endVerse: number;
  verses: Verse[];
  commentary: string;
  reference?: string;
  coversWholeChapters?: boolean;
  // Why the verses are missing, when they are.
  versesUnavailable?: 'rate_limited' | 'error' | null;
  translationCode: string;
  copyrightNotice: string;
}
