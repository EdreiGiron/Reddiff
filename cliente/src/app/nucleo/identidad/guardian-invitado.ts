import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { ServicioSesion } from './servicio-sesion';

export const guardianInvitado: CanActivateFn = () => {
  const sesion = inject(ServicioSesion);
  const enrutador = inject(Router);

  return sesion
    .asegurarSesion()
    .pipe(map((usuario) => (usuario ? enrutador.createUrlTree(['/']) : true)));
};
