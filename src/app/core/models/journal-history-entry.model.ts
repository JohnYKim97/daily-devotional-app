export interface JournalHistoryEntry {
  id: number;
  date: string;
  book: string;
  chapter: number;
  endChapter: number;
  startVerse: number;
  endVerse: number;
  favoriteVerse: number | null;
  notes: string;
}
