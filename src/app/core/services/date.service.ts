import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DateService {
  private _selectedDate = signal<string>(this.getTodayDate());
  readonly selectedDate = this._selectedDate.asReadonly();

  private getTodayDate(): string {
    const today = new Date();

    return today.toISOString().split('T')[0];
  }

  setDate(date: string): void {
    this._selectedDate.set(date);
  }

  previousDay(): void {
    const currentDate = new Date(this._selectedDate() + 'T00:00:00');
    currentDate.setDate(currentDate.getDate() - 1);
    this._selectedDate.set(this.formatDate(currentDate));
  }

  nextDay(): void {
    const currentDate = new Date(this._selectedDate() + 'T00:00:00');
    currentDate.setDate(currentDate.getDate() + 1);
    this._selectedDate.set(this.formatDate(currentDate));
  }

  private formatDate(date: Date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
