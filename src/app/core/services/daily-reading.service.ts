import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DailyReading } from '../models/daily-reading.model';
import { ImportReadingsResponse } from '../models/import-readings-response';

@Injectable({
  providedIn: 'root',
})
export class DailyReadingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5184/api/DailyReading';

  getReadingByDate(date: string): Observable<DailyReading> {
    return this.http.get<DailyReading>(`${this.apiUrl}/${date}`);
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
}
