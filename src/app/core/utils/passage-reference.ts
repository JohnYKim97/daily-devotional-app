import { PassageReferenceParts } from '../models/passage-reference-parts.model';

export function formatPassageReference(reading: PassageReferenceParts): string {
  const { book, chapter, endChapter, startVerse, endVerse } = reading;

  if (chapter === endChapter) {
    const verses = startVerse === endVerse ? `${startVerse}` : `${startVerse}-${endVerse}`;
    return `${book} ${chapter}:${verses}`;
  }

  return `${book} ${chapter}:${startVerse}-${endChapter}:${endVerse}`;
}
