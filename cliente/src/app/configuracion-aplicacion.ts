import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { interceptorApiSegura } from './nucleo/http/interceptor-api-segura';
import { rutasAplicacion } from './rutas-aplicacion';

export const configuracionAplicacion: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([interceptorApiSegura])),
    provideRouter(rutasAplicacion, withComponentInputBinding()),
  ],
};
