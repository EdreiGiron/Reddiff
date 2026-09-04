import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';

@Component({
  selector: 'rd-pagina-inicio-sesion',
  imports: [ReactiveFormsModule],
  templateUrl: './pagina-inicio-sesion.html',
  styleUrl: './pagina-inicio-sesion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaInicioSesion {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioSesion = inject(ServicioSesion);
  private readonly enrutador = inject(Router);
  private readonly ruta = inject(ActivatedRoute);

  protected readonly enviando = signal(false);
  protected readonly mostrarContrasena = signal(false);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly rutaActualizada =
    this.ruta.snapshot.queryParamMap.get('cuentaActualizada') === 'true';
  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    nombreUsuario: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
    contrasena: ['', [Validators.required, Validators.minLength(12), Validators.maxLength(128)]],
  });

  protected alternarVisibilidadContrasena(): void {
    this.mostrarContrasena.update((visible) => !visible);
  }

  protected iniciarSesion(): void {
    this.mensajeError.set(null);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.servicioSesion
      .iniciarSesion(this.formulario.getRawValue())
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => void this.enrutador.navigateByUrl(this.obtenerRutaRetorno()),
        error: (error: unknown) => {
          this.mensajeError.set(
            obtenerMensajeError(
              error,
              'No fue posible iniciar sesión. Verifica tus credenciales e inténtalo de nuevo.',
            ),
          );
          this.formulario.controls.contrasena.reset();
        },
      });
  }

  private obtenerRutaRetorno(): string {
    const retorno = this.ruta.snapshot.queryParamMap.get('retorno');
    return retorno?.startsWith('/') && !retorno.startsWith('//') ? retorno : '/';
  }
}
