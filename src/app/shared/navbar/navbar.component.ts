import { Component, inject, viewChild } from '@angular/core';

import { AuthService } from '../../core/services/auth.service';
import { ImportScheduleComponent } from '../../features/admin/import-schedule/import-schedule.component';

@Component({
  selector: 'app-navbar',
  imports: [ImportScheduleComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  protected authService = inject(AuthService);

  private importDialog = viewChild.required(ImportScheduleComponent);

  openImportDialog(): void {
    this.importDialog().open();
  }
}
