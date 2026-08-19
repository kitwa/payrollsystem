import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const user = auth.currentUser();
  if (user?.token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${user.token}` } });
  }
  return next(req);
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) auth.logout();
      return throwError(() => error);
    })
  );
};
