import { Verse } from './verse.model';

// The verses of a passage as returned by /api/translations/{code}/passage.
export interface Passage {
  translationCode: string;
  copyrightNotice: string;
  // A link the license requires beside the copyright notice, e.g. to the publisher's site.
  attributionLinkText?: string | null;
  attributionLinkUrl?: string | null;
  bookId: number;
  book: string;
  reference: string;
  coversWholeChapters: boolean;
  verses: Verse[];
}
