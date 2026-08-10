import { Passage } from '../../core/models/passage.model';

export const TODAY_READING: Passage = {
  book: 'Genesis',
  chapter: 1,
  startVerse: 1,
  endVerse: 5,
  date: '2026-08-10',

  verses: [
    {
      number: 1,
      text: 'In the beginning God created the heavens and the earth.',
    },
    {
      number: 2,
      text: 'The earth was without form and void, and darkness was over the face of the deep.',
    },
    {
      number: 3,
      text: 'And God said, "Let there be light," and there was light.',
    },
    {
      number: 4,
      text: 'And God saw that the light was good. And God separated the light from the darkness.',
    },
    {
      number: 5,
      text: 'God called the light Day, and the darkness he called Night.',
    },
  ],
};
