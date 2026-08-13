export interface Journal {
  id?: number;
  date: string;
  passageReference: string;
  favoriteVerse?: number | null;
  notes: string;
}
