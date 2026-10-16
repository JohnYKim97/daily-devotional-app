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
  versesUnavailable?: 'rate_limited' | 'too_long' | 'error' | null;
  translationCode: string;
  copyrightNotice: string;
  // A link the license requires beside the copyright notice, e.g. to the publisher's site.
  attributionLinkText?: string | null;
  attributionLinkUrl?: string | null;
}
