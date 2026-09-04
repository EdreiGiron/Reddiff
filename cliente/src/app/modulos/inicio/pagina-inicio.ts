import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';

@Component({
  selector: 'rd-pagina-inicio',
  imports: [RouterLink],
  templateUrl: './pagina-inicio.html',
  styleUrl: './pagina-inicio.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaInicio {
  protected readonly servicioSesion = inject(ServicioSesion);
}
