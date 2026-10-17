import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Verse } from '../../../../core/models/verse.model';
import { VerseListComponent } from './verse-list.component';

const verses: Verse[] = [
  {
    chapter: 1,
    number: 1,
    text: 'In the beginning was the Word.',
    heading: 'The Word Became Flesh',
  },
  {
    chapter: 1,
    number: 4,
    text: 'In him was life,[[1]] and the life was the light of men.',
    footnotes: ['Or *was not any thing made*'],
  },
  {
    chapter: 1,
    number: 11,
    text: 'He came to his own,[[1]] and his own people[[2]] did not receive him.',
    footnotes: ['Greek *to his own things*', '*People* is implied in Greek'],
  },
];

describe('VerseListComponent', () => {
  let fixture: ComponentFixture<VerseListComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerseListComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(VerseListComponent);
    fixture.componentRef.setInput('verses', verses);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders a section heading above its verse', () => {
    const heading = element.querySelector('h2.verse-heading-title');

    expect(heading?.textContent?.trim()).toBe('The Word Became Flesh');
  });

  it('renders footnote markers lettered across the passage', () => {
    const labels = Array.from(element.querySelectorAll('button.footnote-marker')).map((button) =>
      button.getAttribute('aria-label'),
    );

    expect(labels).toEqual(['Footnote a', 'Footnote b', 'Footnote c']);
  });

  it('keeps verse text on both sides of a marker', () => {
    const text = element.querySelectorAll('p.verse')[1].textContent ?? '';

    expect(text).toContain('In him was life,');
    expect(text).toContain('and the life was the light of men.');
    expect(text).not.toContain('[[');
  });

  it('hides footnote text until its marker is activated', async () => {
    const marker = element.querySelector<HTMLButtonElement>('button.footnote-marker')!;
    const note = element.querySelector<HTMLElement>('p.footnote')!;

    expect(note.hidden).toBe(true);
    expect(marker.getAttribute('aria-expanded')).toBe('false');
    expect(marker.getAttribute('aria-controls')).toBe(note.id);

    marker.click();
    await fixture.whenStable();

    expect(note.hidden).toBe(false);
    expect(marker.getAttribute('aria-expanded')).toBe('true');
    expect(note.textContent?.replace(/\s+/g, ' ')).toContain('Or was not any thing made');
    expect(note.querySelector('i')?.textContent).toBe('was not any thing made');

    marker.click();
    await fixture.whenStable();

    expect(note.hidden).toBe(true);
  });

  describe('highlighting the daily passage', () => {
    const chapter: Verse[] = Array.from({ length: 6 }, (_, i) => ({
      chapter: 10,
      number: i + 1,
      text: `Verse ${i + 1}.`,
    }));

    beforeEach(async () => {
      fixture.componentRef.setInput('verses', chapter);
      fixture.componentRef.setInput('highlight', {
        chapter: 10,
        startVerse: 3,
        endChapter: 10,
        endVerse: 5,
      });
      await fixture.whenStable();
    });

    it('marks only the verses of the daily passage, in one labelled group', () => {
      const group = element.querySelector('.verse-group-highlighted')!;
      const numbers = Array.from(group.querySelectorAll('.verse-number')).map((n) =>
        n.textContent?.trim(),
      );

      expect(element.querySelectorAll('.verse-group-highlighted').length).toBe(1);
      expect(numbers).toEqual(['3', '4', '5']);
      expect(group.getAttribute('aria-label')).toBe('Today’s passage');
      expect(group.querySelector('.verse-group-label')?.textContent).toContain('Today');
    });

    it('names the daily passage in the label when a reference is given', async () => {
      fixture.componentRef.setInput('highlightReference', 'Jeremiah 10:3-5');
      await fixture.whenStable();

      expect(
        element.querySelector('.verse-group-label')?.textContent?.replace(/\s+/g, ' ').trim(),
      ).toBe('Today’s passage · Jeremiah 10:3-5');
    });

    it('leaves the surrounding verses unmarked', () => {
      const unmarked = Array.from(
        element.querySelectorAll('.verse-group:not(.verse-group-highlighted) .verse-number'),
      ).map((n) => n.textContent?.trim());

      expect(unmarked).toEqual(['1', '2', '6']);
    });

    it('marks nothing when no range is given', async () => {
      fixture.componentRef.setInput('highlight', null);
      await fixture.whenStable();

      expect(element.querySelector('.verse-group-highlighted')).toBeNull();
    });

    it('handles a range that spans two chapters', async () => {
      fixture.componentRef.setInput('verses', [
        { chapter: 1, number: 29, text: 'a' },
        { chapter: 1, number: 30, text: 'b' },
        { chapter: 1, number: 31, text: 'c' },
        { chapter: 2, number: 1, text: 'd' },
        { chapter: 2, number: 2, text: 'e' },
      ]);
      fixture.componentRef.setInput('highlight', {
        chapter: 1,
        startVerse: 30,
        endChapter: 2,
        endVerse: 1,
      });
      await fixture.whenStable();

      const numbers = Array.from(
        element.querySelectorAll('.verse-group-highlighted .verse-number'),
      ).map((n) => n.textContent?.trim());

      expect(numbers).toEqual(['30', '31', '1']);
    });
  });
});
