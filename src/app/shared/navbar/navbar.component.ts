import { Component, inject, viewChild } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { DateService } from '../../core/services/date.service';
import { ImportScheduleComponent } from '../../features/admin/import-schedule/import-schedule.component';

@Component({
  selector: 'app-navbar',
  imports: [ImportScheduleComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  protected authService = inject(AuthService);
  private dateService = inject(DateService);
  private router = inject(Router);

  private importDialog = viewChild.required(ImportScheduleComponent);

  openImportDialog(): void {
    this.importDialog().open();
  }

  goToToday(): void {
    this.dateService.resetToToday();
    this.router.navigateByUrl('/');
  }
}
