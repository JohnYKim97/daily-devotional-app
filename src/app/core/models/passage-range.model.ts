// A span of verses, possibly across chapters (e.g. Genesis 1:30 to 2:3).
export interface PassageRange {
  chapter: number;
  startVerse: number;
  endChapter: number;
  endVerse: number;
}
