import { Verse } from '../models/verse.model';

export interface DailyReading {
  id: number;
  date: string;
  book: string;
  chapter: number;
  startVerse: number;
  endVerse: number;
  verses: Verse[];
  commentary: string;
}
