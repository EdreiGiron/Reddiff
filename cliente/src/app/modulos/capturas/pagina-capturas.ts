import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { DispositivoResumen } from '../dispositivos/modelos-dispositivos';
import { ServicioDispositivos } from '../dispositivos/servicio-dispositivos';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import {
  CapturaResumen,
  CapturaRemotaResultado,
  CargaArchivoResultado,
  VersionConfiguracionDetalle,
  VersionConfiguracionResumen,
} from './modelos-capturas';
import { ServicioCapturas } from './servicio-capturas';

type VistaEvidencia = 'versiones' | 'capturas';

@Component({
  selector: 'rd-pagina-capturas',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './pagina-capturas.html',
  styleUrl: './pagina-capturas.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaCapturas implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioDispositivos = inject(ServicioDispositivos);
  private readonly servicioCapturas = inject(ServicioCapturas);

  @ViewChild('selectorArchivo') private selectorArchivo?: ElementRef<HTMLInputElement>;

  protected readonly dispositivos = signal<DispositivoResumen[]>([]);
  protected readonly capturas = signal<CapturaResumen[]>([]);
  protected readonly versiones = signal<VersionConfiguracionResumen[]>([]);
  protected readonly archivoSeleccionado = signal<File | null>(null);
  protected readonly versionDetalle = signal<VersionConfiguracionDetalle | null>(null);
  protected readonly cargando = signal(true);
  protected readonly cargandoDetalle = signal(false);
  protected readonly guardando = signal(false);
  protected readonly capturandoRemotamente = signal(false);
  protected readonly vista = signal<VistaEvidencia>('versiones');
  protected readonly filtroDispositivo = signal(0);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);

  protected readonly dispositivosAutorizados = computed(() =>
    this.dispositivos().filter((dispositivo) => dispositivo.estado === 'Autorizado'),
  );

  protected readonly dispositivosCapturables = computed(() =>
    this.dispositivos().filter(
      (dispositivo) =>
        dispositivo.estado === 'Autorizado' &&
        dispositivo.protocolo === 'Ssh' &&
        dispositivo.accesoRemotoConfigurado,
    ),
  );

  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    dispositivoId: [0, [Validators.required, Validators.min(1)]],
    comentario: ['', [Validators.maxLength(300)]],
  });

  protected readonly formularioRemoto = this.constructorFormulario.nonNullable.group({
    dispositivoId: [0, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.cargarTodo();
  }

  protected seleccionarArchivo(evento: Event): void {
    this.mensajeError.set(null);
    const archivo = (evento.target as HTMLInputElement).files?.item(0) ?? null;
    if (!archivo) {
      this.archivoSeleccionado.set(null);
      return;
    }

    const extension = archivo.name.toLocaleLowerCase('es').split('.').pop();
    if (!extension || !['txt', 'cfg', 'conf', 'config'].includes(extension)) {
      this.archivoSeleccionado.set(null);
      this.mensajeError.set('Selecciona un archivo .txt, .cfg, .conf o .config.');
      this.limpiarSelectorArchivo();
      return;
    }

    if (archivo.size > 5_000_000) {
      this.archivoSeleccionado.set(null);
      this.mensajeError.set('El archivo supera el límite de 5 MB.');
      this.limpiarSelectorArchivo();
      return;
    }

    this.archivoSeleccionado.set(archivo);
  }

  protected cargarArchivo(): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
    const archivo = this.archivoSeleccionado();
    if (this.formulario.invalid || !archivo) {
      this.formulario.markAllAsTouched();
      if (!archivo) {
        this.mensajeError.set('Selecciona el archivo de configuración que deseas registrar.');
      }
      return;
    }

    const valores = this.formulario.getRawValue();
    this.guardando.set(true);
    this.servicioCapturas
      .cargarArchivo(valores.dispositivoId, archivo, valores.comentario.trim())
      .pipe(finalize(() => this.guardando.set(false)))
      .subscribe({
        next: (resultado) => {
          this.registrarResultado(resultado);
          this.formulario.controls.comentario.reset('');
          this.archivoSeleccionado.set(null);
          this.limpiarSelectorArchivo();
          this.mensajeExito.set(
            `Se registró la versión ${resultado.version.numero} con su huella de integridad.`,
          );
          this.vista.set('versiones');
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible registrar el archivo de configuración.'),
          ),
      });
  }

  protected capturarRemotamente(): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
    if (this.formularioRemoto.invalid) {
      this.formularioRemoto.markAllAsTouched();
      this.mensajeError.set('Selecciona un dispositivo SSH con acceso remoto configurado.');
      return;
    }

    const dispositivoId = this.formularioRemoto.controls.dispositivoId.value;
    this.capturandoRemotamente.set(true);
    this.servicioCapturas
      .capturarRemotamente(dispositivoId)
      .pipe(finalize(() => this.capturandoRemotamente.set(false)))
      .subscribe({
        next: (resultado) => {
          this.registrarResultado(resultado);
          this.mensajeExito.set(
            `La captura SSH creó la versión ${resultado.version.numero} con su huella de integridad.`,
          );
          this.vista.set('versiones');
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible capturar la configuración mediante SSH.'),
          ),
      });
  }

  protected cambiarFiltro(evento: Event): void {
    this.filtroDispositivo.set(Number((evento.target as HTMLSelectElement).value));
    this.cargarEvidencia();
  }

  protected cambiarVista(vista: VistaEvidencia): void {
    this.vista.set(vista);
  }

  protected abrirDetalle(versionId: number): void {
    this.mensajeError.set(null);
    this.cargandoDetalle.set(true);
    this.servicioCapturas
      .obtenerVersion(versionId)
      .pipe(finalize(() => this.cargandoDetalle.set(false)))
      .subscribe({
        next: (version) => this.versionDetalle.set(version),
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible consultar el contenido de la versión.'),
          ),
      });
  }

  protected cerrarDetalle(): void {
    if (!this.cargandoDetalle()) {
      this.versionDetalle.set(null);
    }
  }

  protected abreviarHash(hash: string): string {
    return `${hash.slice(0, 12)}…${hash.slice(-8)}`;
  }

  protected etiquetaOrigen(origen: string): string {
    const etiquetas: Record<string, string> = {
      Archivo: 'Archivo',
      CapturaSsh: 'SSH',
      CapturaNetconf: 'NETCONF',
    };
    return etiquetas[origen] ?? origen;
  }

  private cargarTodo(): void {
    this.cargando.set(true);
    this.mensajeError.set(null);
    forkJoin({
      dispositivos: this.servicioDispositivos.listar(),
      capturas: this.servicioCapturas.listarCapturas(),
      versiones: this.servicioCapturas.listarVersiones(),
    })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ({ dispositivos, capturas, versiones }) => {
          this.dispositivos.set(dispositivos);
          this.capturas.set(capturas);
          this.versiones.set(versiones);
          const primerAutorizado = dispositivos.find(
            (dispositivo) => dispositivo.estado === 'Autorizado',
          );
          if (primerAutorizado && !this.formulario.controls.dispositivoId.value) {
            this.formulario.controls.dispositivoId.setValue(primerAutorizado.id);
          }
          const primerCapturable = dispositivos.find(
            (dispositivo) =>
              dispositivo.estado === 'Autorizado' &&
              dispositivo.protocolo === 'Ssh' &&
              dispositivo.accesoRemotoConfigurado,
          );
          if (primerCapturable && !this.formularioRemoto.controls.dispositivoId.value) {
            this.formularioRemoto.controls.dispositivoId.setValue(primerCapturable.id);
          }
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cargar las capturas y versiones.'),
          ),
      });
  }

  private cargarEvidencia(): void {
    this.cargando.set(true);
    this.mensajeError.set(null);
    const dispositivoId = this.filtroDispositivo() || undefined;
    forkJoin({
      capturas: this.servicioCapturas.listarCapturas(dispositivoId),
      versiones: this.servicioCapturas.listarVersiones(dispositivoId),
    })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ({ capturas, versiones }) => {
          this.capturas.set(capturas);
          this.versiones.set(versiones);
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible actualizar la evidencia.'),
          ),
      });
  }

  private limpiarSelectorArchivo(): void {
    if (this.selectorArchivo) {
      this.selectorArchivo.nativeElement.value = '';
    }
  }

  private registrarResultado(resultado: CargaArchivoResultado | CapturaRemotaResultado): void {
    this.capturas.update((capturas) => [resultado.captura, ...capturas]);
    this.versiones.update((versiones) => [resultado.version, ...versiones]);
  }
}
