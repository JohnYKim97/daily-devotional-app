import { Component, inject, signal, computed, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DatePipe } from '@angular/common';
import { Router } from '@angular/router';

import { DailyReadingService } from '../../core/services/daily-reading.service';
import { DateService } from '../../core/services/date.service';
import { NotificationService } from '../../core/services/notification.service';
import { ScheduleEntry } from '../../core/models/schedule-entry.model';
import { CalendarDay } from '../../core/models/calendar-day.model';
import { formatPassageReference } from '../../core/utils/passage-reference';

@Component({
  selector: 'app-schedule',
  imports: [DatePipe],
  templateUrl: './schedule.component.html',
  styleUrl: './schedule.component.scss',
})
export class ScheduleComponent {
  private readingService = inject(DailyReadingService);
  private dateService = inject(DateService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  protected readonly today = this.dateService.getToday();

  protected readonly schedule = signal<ScheduleEntry[]>([]);

  protected readonly viewedMonth = signal(this.monthOf(this.today));

  protected readonly monthLabel = computed(() => {
    const { year, month } = this.viewedMonth();
    return new Date(year, month, 1).toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    });
  });

  protected readonly calendarWeeks = computed(() => {
    const { year, month } = this.viewedMonth();
    const entriesByDate = new Map(this.schedule().map((entry) => [entry.date, entry]));

    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const leadingBlanks = new Date(year, month, 1).getDay();

    const days: (CalendarDay | null)[] = Array(leadingBlanks).fill(null);

    for (let day = 1; day <= daysInMonth; day++) {
      const date = this.formatDate(year, month, day);
      days.push({ date, dayNumber: day, entry: entriesByDate.get(date) ?? null });
    }

    while (days.length % 7 !== 0) {
      days.push(null);
    }

    const weeks: (CalendarDay | null)[][] = [];

    for (let i = 0; i < days.length; i += 7) {
      weeks.push(days.slice(i, i + 7));
    }

    return weeks;
  });

  protected readonly groupedSchedule = computed(() => {
    const groups = new Map<string, ScheduleEntry[]>();

    for (const entry of this.schedule()) {
      const key = this.monthLabelOf(entry.date);
      const entries = groups.get(key) ?? [];
      entries.push(entry);
      groups.set(key, entries);
    }

    return Array.from(groups.entries()).map(([month, entries]) => ({ month, entries }));
  });

  private hasScrolledToToday = false;

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.readingService.getSchedule().subscribe({
        next: (schedule) => this.schedule.set(schedule),
        error: (error) => {
          console.error('Error loading schedule: ', error);
          this.notificationService.show('Could not load the reading schedule.', 'error');
        },
      });
    }

    effect(() => {
      const entries = this.schedule();

      if (!isPlatformBrowser(this.platformId) || this.hasScrolledToToday || entries.length === 0) {
        return;
      }

      if (this.scrollToToday()) {
        this.hasScrolledToToday = true;
      }
    });
  }

  dayStatus(date: string): 'past' | 'today' | 'future' {
    if (date === this.today) {
      return 'today';
    }

    return date < this.today ? 'past' : 'future';
  }

  passageReference(entry: ScheduleEntry): string {
    return formatPassageReference(entry);
  }

  goToDate(date: string): void {
    this.dateService.setDate(date);
    this.router.navigateByUrl('/');
  }

  previousMonth(): void {
    this.viewedMonth.update(({ year, month }) =>
      month === 0 ? { year: year - 1, month: 11 } : { year, month: month - 1 },
    );
  }

  nextMonth(): void {
    this.viewedMonth.update(({ year, month }) =>
      month === 11 ? { year: year + 1, month: 0 } : { year, month: month + 1 },
    );
  }

  scrollToToday(): boolean {
    const todayMonth = this.monthOf(this.today);
    const viewed = this.viewedMonth();

    if (viewed.year !== todayMonth.year || viewed.month !== todayMonth.month) {
      this.viewedMonth.set(todayMonth);
    }

    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    queueMicrotask(() => {
      const candidates = [
        document.getElementById('today-schedule-entry-desktop'),
        document.getElementById('today-schedule-entry-mobile'),
      ];

      const visible = candidates.find((el): el is HTMLElement => !!el && el.offsetParent !== null);

      visible?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });

    return true;
  }

  private monthOf(date: string): { year: number; month: number } {
    const [year, month] = date.split('-').map(Number);

    return { year, month: month - 1 };
  }

  private monthLabelOf(date: string): string {
    return new Date(`${date}T00:00:00`).toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    });
  }

  private formatDate(year: number, month: number, day: number): string {
    const mm = String(month + 1).padStart(2, '0');
    const dd = String(day).padStart(2, '0');

    return `${year}-${mm}-${dd}`;
  }
}
