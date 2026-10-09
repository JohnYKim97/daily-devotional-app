import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ReadingComponent } from './reading.component';
import { DailyReading } from '../../core/models/daily-reading.model';
import { Passage } from '../../core/models/passage.model';
import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DailyReadingStateService } from '../../core/services/daily-reading-state.service';
import { SettingsService } from '../../core/services/settings.service';
import { TranslationService } from '../../core/services/translation.service';

const reading: DailyReading = {
  id: 7,
  date: '2026-10-08',
  bookId: 24,
  book: 'Jeremiah',
  chapter: 10,
  endChapter: 10,
  startVerse: 3,
  endVerse: 4,
  commentary: '',
  translationCode: 'ESV',
  copyrightNotice: 'ESV notice',
  coversWholeChapters: false,
  verses: [
    { chapter: 10, number: 3, text: 'Verse three.' },
    { chapter: 10, number: 4, text: 'Verse four.' },
  ],
};

const fullChapter: Passage = {
  translationCode: 'ESV',
  copyrightNotice: 'ESV notice',
  bookId: 24,
  book: 'Jeremiah',
  reference: 'Jeremiah 10',
  coversWholeChapters: true,
  verses: [1, 2, 3, 4, 5].map((number) => ({ chapter: 10, number, text: `Verse ${number}.` })),
};

describe('ReadingComponent', () => {
  let fixture: ComponentFixture<ReadingComponent>;
  let element: HTMLElement;
  let state: {
    reading: ReturnType<typeof signal<DailyReading | null>>;
    loading: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<boolean>>;
    rateLimited: ReturnType<typeof signal<boolean>>;
    loadReading: ReturnType<typeof vi.fn>;
  };
  let getFullChapters: ReturnType<typeof vi.fn>;
  let select: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    select = vi.fn();
    state = {
      reading: signal<DailyReading | null>(reading),
      loading: signal(false),
      error: signal(false),
      rateLimited: signal(false),
      loadReading: vi.fn(),
    };
    getFullChapters = vi.fn(() => of(fullChapter));

    await TestBed.configureTestingModule({
      imports: [ReadingComponent],
      providers: [
        { provide: DailyReadingStateService, useValue: state },
        { provide: DailyReadingService, useValue: { getFullChapters } },
        {
          provide: TranslationService,
          useValue: {
            loaded: signal(true),
            selectedCode: () => 'ESV',
            translations: signal([
              { id: 1, code: 'ESV', name: 'English Standard Version' },
              { id: 2, code: 'KJV', name: 'King James Version' },
            ]),
            select,
          },
        },
        { provide: SettingsService, useValue: { enableAiCommentary: signal(false) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReadingComponent);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  const text = (selector: string) => element.querySelector(selector)?.textContent?.trim();

  it('puts the full-chapter button in the passage header', () => {
    const button = element.querySelector('app-passage-header .full-chapter-button');

    expect(button?.textContent?.trim()).toBe('Read full chapter');
  });

  it('lets the reader switch the Bible version on the page', () => {
    const dropdown = element.querySelector<HTMLSelectElement>('.version-select')!;

    expect(Array.from(dropdown.options).map((o) => o.textContent?.trim())).toEqual(['ESV', 'KJV']);
    expect(dropdown.value).toBe('1');

    dropdown.value = '2';
    dropdown.dispatchEvent(new Event('change'));

    expect(select).toHaveBeenCalledWith(2);
  });

  it('hides the button when the daily passage is already the whole chapter', async () => {
    state.reading.set({ ...reading, coversWholeChapters: true });
    await fixture.whenStable();

    expect(element.querySelector('.full-chapter-button')).toBeNull();
  });

  it('says "chapters" for a reading that spans chapters', async () => {
    state.reading.set({ ...reading, endChapter: 11 });
    await fixture.whenStable();

    expect(text('.full-chapter-button')).toBe('Read full chapters');
  });

  it('loads the whole chapter, marks the daily passage, and toggles back', async () => {
    element.querySelector<HTMLButtonElement>('.full-chapter-button')!.click();
    await fixture.whenStable();

    // The header now names the whole chapter, and the highlight names the daily passage.
    expect(text('.passage-title')).toBe('Jeremiah 10');
    expect(text('.verse-group-label')?.replace(/\s+/g, ' ')).toBe(
      'Today’s passage · Jeremiah 10:3-4',
    );
    expect(element.querySelector('.verse-group-highlighted')?.getAttribute('aria-label')).toBe(
      'Today’s passage · Jeremiah 10:3-4',
    );

    expect(getFullChapters).toHaveBeenCalledWith('ESV', 24, 10, 10);
    expect(element.querySelectorAll('p.verse').length).toBe(5);
    expect(text('.full-chapter-button')).toBe('Show daily passage only');

    const marked = Array.from(
      element.querySelectorAll('.verse-group-highlighted .verse-number'),
    ).map((n) => n.textContent?.trim());
    expect(marked).toEqual(['3', '4']);

    // The user has scrolled down the full chapter.
    const verseCard = element.querySelector<HTMLElement>('.verse-card')!;
    verseCard.scrollTop = 400;

    element.querySelector<HTMLButtonElement>('.full-chapter-button')!.click();
    await fixture.whenStable();

    expect(verseCard.scrollTop).toBe(0);
    expect(text('.passage-title')).toBe('Jeremiah 10:3-4');
    expect(element.querySelectorAll('p.verse').length).toBe(2);
    expect(element.querySelector('.verse-group-highlighted')).toBeNull();
    expect(text('.full-chapter-button')).toBe('Read full chapter');
  });

  it('keeps the full chapter open when the Bible version changes', async () => {
    element.querySelector<HTMLButtonElement>('.full-chapter-button')!.click();
    await fixture.whenStable();

    // Switching version loads the reading again, as a new object in the other version.
    state.reading.set(null);
    state.reading.set({ ...reading, translationCode: 'KJV' });
    await fixture.whenStable();

    expect(getFullChapters).toHaveBeenLastCalledWith('KJV', 24, 10, 10);
    expect(text('.passage-title')).toBe('Jeremiah 10');
    expect(text('.full-chapter-button')).toBe('Show daily passage only');
  });

  it('shows a clear message, with a retry, when the API is rate limiting', async () => {
    state.rateLimited.set(true);
    state.error.set(true);
    state.reading.set(null);
    await fixture.whenStable();

    expect(text('.reading-error h2')).toBe('Too many requests');
    expect(element.querySelector('.reading-error')?.getAttribute('role')).toBe('alert');

    state.loadReading.mockClear();
    element.querySelector<HTMLButtonElement>('.reading-error .retry-button')!.click();

    expect(state.loadReading).toHaveBeenCalled();
  });

  it('explains missing verses when the Bible provider is rate limited', async () => {
    state.reading.set({
      ...reading,
      verses: [],
      translationCode: '',
      versesUnavailable: 'rate_limited',
    });
    await fixture.whenStable();

    expect(text('.verses-unavailable')).toContain('requested too often');
    expect(element.querySelector('.full-chapter-button')).toBeNull();
  });

  it('shows a generic message when verses fail for another reason', async () => {
    state.reading.set({ ...reading, verses: [], translationCode: '', versesUnavailable: 'error' });
    await fixture.whenStable();

    expect(text('.verses-unavailable')).toContain('could not be loaded');
  });
});
