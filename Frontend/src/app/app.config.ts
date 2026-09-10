import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { jwtInterceptor, errorInterceptor } from './core/interceptors/http.interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([jwtInterceptor, errorInterceptor])),
    provideServiceWorker('ngsw-worker.js', { enabled: true })
  ]
};
