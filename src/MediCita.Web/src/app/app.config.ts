import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeEs from '@angular/common/locales/es-DO';
import { ApplicationConfig, LOCALE_ID } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { interceptorDeSesionVencida, interceptorDeToken } from './nucleo/interceptores';

registerLocaleData(localeEs);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([interceptorDeToken, interceptorDeSesionVencida])),
    { provide: LOCALE_ID, useValue: 'es-DO' },
  ],
};
