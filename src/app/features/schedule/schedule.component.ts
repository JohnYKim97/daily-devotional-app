import { Component, inject, signal, computed, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DatePipe } from '@angular/common';
import { Router } from '@angular/router';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DateService } from '../../core/services/date.service';
import { ScheduleEntry } from '../../core/models/schedule-entry.model';

@Component({
  selector: 'app-schedule',
  imports: [DatePipe],
  templateUrl: './schedule.component.html',
  styleUrl: './schedule.component.scss',
})
export class ScheduleComponent {
  private readingService = inject(DailyReadingService);
  private dateService = inject(DateService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  protected readonly today = this.dateService.getToday();

  protected readonly schedule = signal<ScheduleEntry[]>([]);

  protected readonly groupedSchedule = computed(() => {
    const groups = new Map<string, ScheduleEntry[]>();

    for (const entry of this.schedule()) {
      const key = this.monthLabel(entry.date);
      const entries = groups.get(key) ?? [];
      entries.push(entry);
      groups.set(key, entries);
    }

    return Array.from(groups.entries()).map(([month, entries]) => ({
      month,
      entries,
    }));
  });

  private hasScrolledToToday = false;

  constructor() {
    this.readingService.getSchedule().subscribe({
      next: (schedule) => this.schedule.set(schedule),
      error: (error) => console.error('Error loading schedule: ', error),
    });

    effect(() => {
      const groups = this.groupedSchedule();

      if (!isPlatformBrowser(this.platformId) || this.hasScrolledToToday || groups.length === 0) {
        return;
      }

      queueMicrotask(() => {
        if (this.scrollToToday()) {
          this.hasScrolledToToday = true;
        }
      });
    });
  }

  status(entry: ScheduleEntry): 'past' | 'today' | 'future' {
    if (entry.date === this.today) {
      return 'today';
    }

    return entry.date < this.today ? 'past' : 'future';
  }

  passageReference(entry: ScheduleEntry): string {
    const verses =
      entry.startVerse === entry.endVerse
        ? `${entry.startVerse}`
        : `${entry.startVerse}-${entry.endVerse}`;

    return `${entry.book} ${entry.chapter}:${verses}`;
  }

  goToDate(date: string): void {
    this.dateService.setDate(date);
    this.router.navigateByUrl('/');
  }

  scrollToToday(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    const element = document.getElementById('today-schedule-entry');

    if (!element) {
      return false;
    }

    element.scrollIntoView({ behavior: 'smooth', block: 'center' });

    return true;
  }

  private monthLabel(date: string): string {
    return new Date(`${date}T00:00:00`).toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    });
  }
}
