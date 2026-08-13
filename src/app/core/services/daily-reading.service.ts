import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DailyReading } from '../models/daily-reading.model';

@Injectable({
  providedIn: 'root',
})
export class DailyReadingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5184/api/DailyReading';

  getReadingByDate(date: string): Observable<DailyReading> {
    return this.http.get<DailyReading>(`${this.apiUrl}/${date}`);
  }
}
