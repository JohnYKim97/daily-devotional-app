import { Component, inject, signal, viewChild } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { DateService } from '../../core/services/date.service';
import { ImportScheduleComponent } from '../../features/admin/import-schedule/import-schedule.component';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, ImportScheduleComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  protected authService = inject(AuthService);
  private dateService = inject(DateService);
  private router = inject(Router);

  private importDialog = viewChild.required(ImportScheduleComponent);

  protected readonly isMenuOpen = signal(false);

  openImportDialog(): void {
    this.closeMenu();
    this.importDialog().open();
  }

  goToToday(): void {
    this.closeMenu();
    this.dateService.resetToToday();
    this.router.navigateByUrl('/');
  }

  toggleMenu(): void {
    this.isMenuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.isMenuOpen.set(false);
  }
}
