import { Service, signal, computed, inject, PLATFORM_ID } from "@angular/core";
import { isPlatformBrowser } from "@angular/common";
import { Router } from "@angular/router";

const TOKEN_STORAGE_KEY = 'daily-devotional-token';

@Service()
export class AuthService {
    private router = inject(Router);
    private platformId = inject(PLATFORM_ID);
    private apiUrl = 'http://localhost:5184/api/auth';

    private _token = signal<string | null>(this.readStoredToken());
    readonly token = this._token.asReadonly();
    readonly isAuthenticated = computed(() => this._token() !== null);

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
    }

    logout(): void {
        if (isPlatformBrowser(this.platformId)) {
            localStorage.removeItem(TOKEN_STORAGE_KEY);
        }

        this._token.set(null);
        this.router.navigateByUrl('/login');
    }
}