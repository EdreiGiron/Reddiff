import { Routes } from '@angular/router';
import { guardianAdministrador } from './nucleo/identidad/guardian-administrador';
import { guardianAutenticacion } from './nucleo/identidad/guardian-autenticacion';
import { guardianInvitado } from './nucleo/identidad/guardian-invitado';

export const rutasAplicacion: Routes = [
  {
    path: 'iniciar-sesion',
    canActivate: [guardianInvitado],
    loadComponent: () =>
      import('./modulos/autenticacion/pagina-inicio-sesion').then(
        (modulo) => modulo.PaginaInicioSesion,
      ),
    title: 'RedDiff | Iniciar sesión',
  },
  {
    path: '',
    canActivate: [guardianAutenticacion],
    loadComponent: () =>
      import('./compartido/disposicion/disposicion-autenticada').then(
        (modulo) => modulo.DisposicionAutenticada,
      ),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./modulos/inicio/pagina-inicio').then((modulo) => modulo.PaginaInicio),
        title: 'RedDiff | Inicio',
      },
      {
        path: 'dispositivos',
        loadComponent: () =>
          import('./modulos/dispositivos/pagina-dispositivos').then(
            (modulo) => modulo.PaginaDispositivos,
          ),
        title: 'RedDiff | Dispositivos',
      },
      {
        path: 'capturas',
        loadComponent: () =>
          import('./modulos/capturas/pagina-capturas').then((modulo) => modulo.PaginaCapturas),
        title: 'RedDiff | Capturas y versiones',
      },
      {
        path: 'usuarios',
        canActivate: [guardianAdministrador],
        loadComponent: () =>
          import('./modulos/usuarios/pagina-usuarios').then((modulo) => modulo.PaginaUsuarios),
        title: 'RedDiff | Usuarios',
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
