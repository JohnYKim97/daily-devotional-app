import { ScheduleEntry } from './schedule-entry.model';

export interface CalendarDay {
  date: string;
  dayNumber: number;
  entry: ScheduleEntry | null;
}
