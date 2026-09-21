import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, map } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { DateService } from '../../core/services/date.service';
import { ImportScheduleComponent } from '../../features/admin/import-schedule/import-schedule.component';
import { SettingsService } from '../../core/services/settings.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, ImportScheduleComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  protected authService = inject(AuthService);
  private dateService = inject(DateService);
  private settingsService = inject(SettingsService);
  private router = inject(Router);

  private importDialog = viewChild.required(ImportScheduleComponent);

  protected readonly isMenuOpen = signal(false);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  protected readonly isOnToday = computed(
    () =>
      this.currentUrl() === '/' && this.dateService.selectedDate() === this.dateService.getToday(),
  );

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
