import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { ServicioSesion } from './servicio-sesion';

export const guardianAdministrador: CanActivateFn = () => {
  const sesion = inject(ServicioSesion);
  const enrutador = inject(Router);

  return sesion
    .asegurarSesion()
    .pipe(
      map((usuario) => (usuario?.rol === 'Administrador' ? true : enrutador.createUrlTree(['/']))),
    );
};
