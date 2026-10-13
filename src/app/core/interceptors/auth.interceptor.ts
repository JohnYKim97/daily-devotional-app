import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const notificationService = inject(NotificationService);
  const token = authService.token();

  if (!token) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    }),
  ).pipe(
    catchError((error: unknown) => {
      // Only the first failing request with this token logs out; later ones see a cleared token.
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        authService.token() === token
      ) {
        authService.logout();
        notificationService.show('Your session has expired. Please sign in again.', 'error');
      }

      return throwError(() => error);
    }),
  );
};
