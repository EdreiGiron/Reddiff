import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { finalize } from 'rxjs';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';

@Component({
  selector: 'rd-disposicion-autenticada',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './disposicion-autenticada.html',
  styleUrl: './disposicion-autenticada.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DisposicionAutenticada {
  private readonly enrutador = inject(Router);
  protected readonly servicioSesion = inject(ServicioSesion);
  protected readonly cerrandoSesion = signal(false);
  protected readonly menuAbierto = signal(false);

  protected alternarMenu(): void {
    this.menuAbierto.update((abierto) => !abierto);
  }

  protected cerrarMenu(): void {
    this.menuAbierto.set(false);
  }

  protected cerrarSesion(): void {
    if (this.cerrandoSesion()) {
      return;
    }

    this.cerrandoSesion.set(true);
    this.servicioSesion
      .cerrarSesion()
      .pipe(finalize(() => this.cerrandoSesion.set(false)))
      .subscribe({
        next: () => void this.enrutador.navigate(['/iniciar-sesion']),
        error: () => void this.enrutador.navigate(['/iniciar-sesion']),
      });
  }
}
