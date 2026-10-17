import { PassageReferenceParts } from '../models/passage-reference-parts.model';

export function formatPassageReference(reading: PassageReferenceParts): string {
  const { book, chapter, endChapter, startVerse, endVerse } = reading;

  if (chapter === endChapter) {
    const verses = startVerse === endVerse ? `${startVerse}` : `${startVerse}-${endVerse}`;
    return `${book} ${chapter}:${verses}`;
  }

  return `${book} ${chapter}:${startVerse}-${endChapter}:${endVerse}`;
}

// The whole chapter(s) a reading falls in, e.g. "Jeremiah 10" or "Genesis 1-2".
export function formatChapterReference(reading: PassageReferenceParts): string {
  const { book, chapter, endChapter } = reading;

  return chapter === endChapter || endChapter < chapter
    ? `${book} ${chapter}`
    : `${book} ${chapter}-${endChapter}`;
}
