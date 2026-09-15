import { Service, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { CurrentUserResponse } from '../models/current-user-response.model';

const TOKEN_STORAGE_KEY = 'daily-devotional-token';

@Service()
export class AuthService {
  private router = inject(Router);
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private apiUrl = 'http://localhost:5184/api/auth';

  private _token = signal<string | null>(this.readStoredToken());
  readonly token = this._token.asReadonly();
  readonly isAuthenticated = computed(() => this._token() !== null);

  private _isAdmin = signal(false);
  readonly isAdmin = this._isAdmin.asReadonly();

  constructor() {
    if (this._token() !== null) {
      this.fetchCurrentUser();
    }
  }

  private fetchCurrentUser(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.http.get<CurrentUserResponse>(`${this.apiUrl}/me`).subscribe({
      next: (user) => this._isAdmin.set(user.isAdmin),
      error: () => this._isAdmin.set(false),
    });
  }

  private readStoredToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }

  loginWithGoogle(): void {
    if (isPlatformBrowser(this.platformId)) {
      window.location.href = `${this.apiUrl}/google`;
    }
  }

  setToken(token: string): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    }

    this._token.set(token);
    this.fetchCurrentUser();
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }

    this._token.set(null);
    this._isAdmin.set(false);
    this.router.navigateByUrl('/login');
  }
}
