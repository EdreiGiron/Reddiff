import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { ServicioProteccionCsrf } from './servicio-proteccion-csrf';

const METODOS_SEGUROS = new Set(['GET', 'HEAD', 'OPTIONS']);

export const interceptorApiSegura: HttpInterceptorFn = (solicitud, siguiente) => {
  if (!solicitud.url.startsWith('/api/')) {
    return siguiente(solicitud);
  }

  const solicitudConCredenciales = solicitud.clone({ withCredentials: true });
  if (METODOS_SEGUROS.has(solicitud.method.toUpperCase())) {
    return siguiente(solicitudConCredenciales);
  }

  const proteccionCsrf = inject(ServicioProteccionCsrf);
  return proteccionCsrf.obtenerToken().pipe(
    switchMap((token) =>
      siguiente(
        solicitudConCredenciales.clone({
          setHeaders: { 'X-CSRF-TOKEN': token },
        }),
      ),
    ),
  );
};
