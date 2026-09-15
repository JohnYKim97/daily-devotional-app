import { Component, inject, signal, viewChild, ElementRef, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { form, FormField, required, submit } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';

import { DailyReadingService } from '../../../core/services/daily-reading.service';
import { ImportReadingsResponse } from '../../../core/models/import-readings-response.model';
import { NotificationService } from '../../../core/services/notification.service';
import { DailyReadingStateService } from '../../../core/services/daily-reading-state.service';
import { DateService } from '../../../core/services/date.service';

@Component({
  selector: 'app-import-schedule',
  imports: [FormField],
  templateUrl: './import-schedule.component.html',
  styleUrl: './import-schedule.component.scss',
})
export class ImportScheduleComponent {
  private readingService = inject(DailyReadingService);
  private notificationService = inject(NotificationService);
  private readingStateService = inject(DailyReadingStateService);
  private dateService = inject(DateService);
  private platformId = inject(PLATFORM_ID);

  private dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly attemptedSubmit = signal(false);
  protected readonly submitting = signal(false);

  protected readonly model = signal({
    startDate: '',
    overwrite: false,
  });

  protected readonly importForm = form(this.model, (schema) => {
    required(schema.startDate, { message: 'A start date is required.' });
  });

  open(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.reset();
    this.dialog().nativeElement.showModal();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === this.dialog().nativeElement) {
      this.close();
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  onSubmit(): void {
    submit(this.importForm, async () => {
      this.attemptedSubmit.set(true);

      const file = this.selectedFile();

      if (!file) {
        return;
      }

      const { startDate, overwrite } = this.model();

      this.submitting.set(true);

      try {
        const response = await firstValueFrom(
          this.readingService.importSchedule(file, startDate, overwrite),
        );

        this.close();
        this.notificationService.show(
          `Imported ${response.importedCount} reading(s) from ${response.startDate} to ${response.endDate}.`,
          'success',
        );

        this.readingStateService.loadReading(this.dateService.selectedDate());
      } catch (err) {
        const httpError = err as { error?: { errors?: string[] } };

        const message =
          httpError.error?.errors?.[0] ?? 'Something went wrong while importing the document.';

        this.notificationService.show(message, 'error');
      } finally {
        this.submitting.set(false);
      }
    });
  }

  private reset(): void {
    this.selectedFile.set(null);
    this.attemptedSubmit.set(false);
    this.model.set({ startDate: '', overwrite: false });
  }
}
