import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';

import { AuthCallbackComponent } from './auth-callback.component';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';

describe('AuthCallbackComponent', () => {
  function setup(queryParams: Record<string, string>) {
    TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } },
        },
      ],
    });

    const navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(AuthCallbackComponent);

    return { fixture, navigateByUrl };
  }

  beforeEach(() => localStorage.clear());

  it('should store the token and go home when a token is present', () => {
    const setToken = vi.spyOn(AuthService.prototype, 'setToken').mockImplementation(() => {});
    const { navigateByUrl } = setup({ token: 'abc' });

    expect(setToken).toHaveBeenCalledWith('abc');
    expect(navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('should show an error and go to login when no token is present', () => {
    const show = vi.spyOn(NotificationService.prototype, 'show');
    const { navigateByUrl } = setup({});

    expect(show).toHaveBeenCalledWith('Sign-in failed. Please try again.', 'error');
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
