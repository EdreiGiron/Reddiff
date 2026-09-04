import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { ServicioSesion } from './servicio-sesion';

export const guardianAutenticacion: CanActivateFn = (_ruta, estado) => {
  const sesion = inject(ServicioSesion);
  const enrutador = inject(Router);

  return sesion.asegurarSesion().pipe(
    map((usuario) =>
      usuario
        ? true
        : enrutador.createUrlTree(['/iniciar-sesion'], {
            queryParams: { retorno: estado.url },
          }),
    ),
  );
};
