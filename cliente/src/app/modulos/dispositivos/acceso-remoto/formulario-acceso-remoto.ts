import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { obtenerMensajeError } from '../../../nucleo/http/modelo-error-api';
import {
  AccesoRemotoResumen,
  ConfigurarAccesoRemotoSolicitud,
  DispositivoResumen,
} from '../modelos-dispositivos';
import { ServicioDispositivos } from '../servicio-dispositivos';

@Component({
  selector: 'rd-formulario-acceso-remoto',
  imports: [ReactiveFormsModule],
  templateUrl: './formulario-acceso-remoto.html',
  styleUrl: './formulario-acceso-remoto.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormularioAccesoRemoto implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioDispositivos = inject(ServicioDispositivos);

  readonly dispositivo = input.required<DispositivoResumen>();
  readonly cerrado = output<void>();
  readonly actualizado = output<AccesoRemotoResumen>();

  protected readonly detalle = signal<AccesoRemotoResumen | null>(null);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly mensajeError = signal<string | null>(null);

  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    usuarioAcceso: ['', [Validators.required, Validators.maxLength(120)]],
    secretoAcceso: ['', [Validators.required, Validators.maxLength(256)]],
    algoritmoClaveHost: ['ssh-ed25519', [Validators.required, Validators.maxLength(100)]],
    huellaClaveHost: ['', [Validators.required, Validators.pattern(/^[0-9a-fA-F]{64}$/)]],
    huellaClaveHostConfirmada: [false, [Validators.requiredTrue]],
  });

  ngOnInit(): void {
    this.cargar();
  }

  protected cerrar(): void {
    if (this.cargando() || this.guardando()) {
      return;
    }

    this.limpiarSecreto();
    this.cerrado.emit();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.mensajeError.set(null);
    this.detalle.set(null);
    this.limpiarSecreto();

    this.servicioDispositivos
      .obtenerAccesoRemoto(this.dispositivo().id)
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (detalle) => {
          this.detalle.set(detalle);
          this.formulario.reset({
            usuarioAcceso: detalle.usuarioAcceso ?? '',
            secretoAcceso: '',
            algoritmoClaveHost: detalle.algoritmoClaveHost ?? 'ssh-ed25519',
            huellaClaveHost: detalle.huellaClaveHost ?? '',
            huellaClaveHostConfirmada: false,
          });
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible consultar el acceso remoto.'),
          ),
      });
  }

  protected guardar(): void {
    this.mensajeError.set(null);
    if (this.formulario.invalid || !this.detalle()) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();
    const solicitud: ConfigurarAccesoRemotoSolicitud = {
      usuarioAcceso: valores.usuarioAcceso.trim(),
      secretoAcceso: valores.secretoAcceso,
      algoritmoClaveHost: valores.algoritmoClaveHost.trim(),
      huellaClaveHost: valores.huellaClaveHost.trim(),
      huellaClaveHostConfirmada: valores.huellaClaveHostConfirmada,
    };

    this.guardando.set(true);
    this.servicioDispositivos
      .configurarAccesoRemoto(this.dispositivo().id, solicitud)
      .pipe(finalize(() => this.guardando.set(false)))
      .subscribe({
        next: (detalle) => {
          this.limpiarSecreto();
          this.actualizado.emit(detalle);
        },
        error: (error: unknown) => {
          this.limpiarSecreto();
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible configurar el acceso remoto.'),
          );
        },
      });
  }

  protected etiquetaProtocolo(): string {
    return this.dispositivo().protocolo === 'Ssh' ? 'SSH' : 'NETCONF';
  }

  protected formatearFecha(fecha: string | null): string {
    if (!fecha) {
      return 'Sin configuración previa';
    }

    return new Intl.DateTimeFormat('es-GT', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(fecha));
  }

  private limpiarSecreto(): void {
    this.formulario.controls.secretoAcceso.setValue('');
  }
}
