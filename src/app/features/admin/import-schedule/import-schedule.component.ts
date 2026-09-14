import { Component, inject, signal } from '@angular/core';
import { form, FormField, required, submit } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';

import { DailyReadingService } from '../../../core/services/daily-reading.service';
import { ImportReadingsResponse } from '../../../core/models/import-readings-response';

@Component({
  selector: 'app-import-schedule',
  imports: [FormField],
  templateUrl: './import-schedule.component.html',
  styleUrl: './import-schedule.component.scss',
})
export class ImportScheduleComponent {
  private readingService = inject(DailyReadingService);

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly attemptedSubmit = signal(false);
  protected readonly result = signal<ImportReadingsResponse | null>(null);
  protected readonly errors = signal<string[]>([]);

  protected readonly model = signal({
    startDate: '',
    overwrite: false,
  });

  protected readonly importForm = form(this.model, (schema) => {
    required(schema.startDate, { message: 'A start date is required.' });
  });

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

      this.result.set(null);
      this.errors.set([]);

      const { startDate, overwrite } = this.model();

      try {
        const response = await firstValueFrom(
          this.readingService.importSchedule(file, startDate, overwrite),
        );

        this.result.set(response);
      } catch (err) {
        const httpError = err as { error?: { errors?: string[] } };

        this.errors.set(
          httpError.error?.errors ?? ['Something went wrong while importing the document.'],
        );
      }
    });
  }
}
