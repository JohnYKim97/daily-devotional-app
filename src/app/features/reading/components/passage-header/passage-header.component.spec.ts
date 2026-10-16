import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PassageHeaderComponent } from './passage-header.component';
import { DailyReading } from '../../../../core/models/daily-reading.model';
import { Verse } from '../../../../core/models/verse.model';
import { SettingsService } from '../../../../core/services/settings.service';

const reading: DailyReading = {
  id: 1,
  date: '2026-10-08',
  bookId: 24,
  book: 'Jeremiah',
  chapter: 10,
  endChapter: 10,
  startVerse: 11,
  endVerse: 25,
  verses: [],
  commentary: '',
  translationCode: 'ESV',
  copyrightNotice: '',
};

@Component({
  imports: [PassageHeaderComponent],
  template: `
    <app-passage-header [reading]="reading" [fullChapterVerses]="fullChapterVerses()">
      <button type="button" class="projected-action">Read full chapter</button>
    </app-passage-header>
  `,
})
class HostComponent {
  reading = reading;
  fullChapterVerses = signal<Verse[] | null>(null);
}

describe('PassageHeaderComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: SettingsService, useValue: { enableAiCommentary: signal(false) } }],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  it('shows the passage reference', () => {
    expect(element.querySelector('.passage-title')?.textContent?.trim()).toBe('Jeremiah 10:11-25');
  });

  it('spans the whole chapter while the full chapter is shown', async () => {
    host.fullChapterVerses.set([1, 2, 27].map((number) => ({ chapter: 10, number, text: '' })));
    await fixture.whenStable();

    expect(element.querySelector('.passage-title')?.textContent?.trim()).toBe('Jeremiah 10:1-27');
  });

  it('places projected actions beside the title, in the same group', () => {
    const group = element.querySelector('.passage-title-group')!;

    expect(group.querySelector('.passage-title')).not.toBeNull();
    expect(group.querySelector('.projected-action')).not.toBeNull();
  });
});
