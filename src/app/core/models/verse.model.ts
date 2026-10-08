export interface Verse {
  chapter: number;
  number: number;
  // Verse text with [[n]] footnote markers, where n is the 1-based position in `footnotes`.
  text: string;
  // Section heading above the verse; several lines are separated by a newline.
  heading?: string | null;
  footnotes?: string[];
}
