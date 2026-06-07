import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  const authReq = req.clone({
    withCredentials: true
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => handleHttpError(error, router, authService))
  );
};

function handleHttpError(error: HttpErrorResponse, router: Router, authService: AuthService) {
  if (error.status === 401) {
    authService.logout();
    const currentUrl = router.url;
    if (currentUrl && !currentUrl.startsWith('/login')) {
      router.navigate(['/login'], { queryParams: { returnUrl: currentUrl } });
    }
  }

  return throwError(() => error);
}
