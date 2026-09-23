import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DailyReading } from '../models/daily-reading.model';
import { ImportReadingsResponse } from '../models/import-readings-response.model';
import { ScheduleEntry } from '../models/schedule-entry.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DailyReadingService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/DailyReading`;

  getReadingByDate(date: string): Observable<DailyReading> {
    return this.http.get<DailyReading>(`${this.apiUrl}/${date}`);
  }

  getSchedule(): Observable<ScheduleEntry[]> {
    return this.http.get<ScheduleEntry[]>(this.apiUrl);
  }

  importSchedule(
    file: File,
    startDate: string,
    overwrite: boolean,
  ): Observable<ImportReadingsResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('startDate', startDate);
    formData.append('overwrite', String(overwrite));

    return this.http.post<ImportReadingsResponse>(`${this.apiUrl}/import`, formData);
  }

  generateCommentary(date: string): Observable<{ commentary: string }> {
    return this.http.post<{ commentary: string }>(`${this.apiUrl}/${date}/commentary`, {});
  }
}
