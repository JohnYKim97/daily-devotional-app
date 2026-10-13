import { Service, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { CurrentUserResponse } from '../models/current-user-response.model';
import { environment } from '../../../environments/environment';

const TOKEN_STORAGE_KEY = 'daily-devotional-token';

@Service()
export class AuthService {
  private router = inject(Router);
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private apiUrl = `${environment.apiUrl}/auth`;

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

    const token = localStorage.getItem(TOKEN_STORAGE_KEY);

    if (token && isTokenExpired(token)) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      return null;
    }

    return token;
  }

  hasValidToken(): boolean {
    const token = this._token();

    if (token && isTokenExpired(token)) {
      this.clearToken();
    }

    return this._token() !== null;
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
    this.clearToken();
    this.router.navigateByUrl('/login');
  }

  private clearToken(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }

    this._token.set(null);
    this._isAdmin.set(false);
  }
}

function isTokenExpired(token: string): boolean {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const { exp } = JSON.parse(atob(payload)) as { exp?: number };

    return typeof exp === 'number' && exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}
