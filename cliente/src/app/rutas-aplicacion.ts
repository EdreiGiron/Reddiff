import { Routes } from '@angular/router';

export const rutasAplicacion: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./modulos/inicio/pagina-inicio').then((modulo) => modulo.PaginaInicio),
    title: 'RedDiff | Inicio',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
