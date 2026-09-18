import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import { VersionConfiguracionResumen } from '../capturas/modelos-capturas';
import { ServicioCapturas } from '../capturas/servicio-capturas';
import { ComparacionDetalle, ComparacionResumen, TipoDiferencia } from './modelos-comparaciones';
import { ServicioComparaciones } from './servicio-comparaciones';

interface DispositivoComparable {
  id: number;
  nombre: string;
  cantidadVersiones: number;
}

@Component({
  selector: 'rd-pagina-comparaciones',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './pagina-comparaciones.html',
  styleUrl: './pagina-comparaciones.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaComparaciones implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioCapturas = inject(ServicioCapturas);
  private readonly servicioComparaciones = inject(ServicioComparaciones);

  protected readonly versiones = signal<VersionConfiguracionResumen[]>([]);
  protected readonly comparaciones = signal<ComparacionResumen[]>([]);
  protected readonly resultado = signal<ComparacionDetalle | null>(null);
  protected readonly dispositivoSeleccionadoId = signal(0);
  protected readonly cargando = signal(true);
  protected readonly comparando = signal(false);
  protected readonly cargandoDetalle = signal(false);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);

  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    dispositivoId: [0, [Validators.required, Validators.min(1)]],
    versionOrigenId: [0, [Validators.required, Validators.min(1)]],
    versionDestinoId: [0, [Validators.required, Validators.min(1)]],
  });

  protected readonly dispositivosComparables = computed<DispositivoComparable[]>(() => {
    const agrupados = new Map<number, DispositivoComparable>();
    for (const version of this.versiones()) {
      const existente = agrupados.get(version.dispositivoId);
      if (existente) {
        existente.cantidadVersiones++;
      } else {
        agrupados.set(version.dispositivoId, {
          id: version.dispositivoId,
          nombre: version.dispositivo,
          cantidadVersiones: 1,
        });
      }
    }

    return [...agrupados.values()]
      .filter((dispositivo) => dispositivo.cantidadVersiones >= 2)
      .sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'));
  });

  protected readonly versionesDispositivo = computed(() => {
    const dispositivoId = this.dispositivoSeleccionadoId();
    return this.versiones()
      .filter((version) => version.dispositivoId === dispositivoId)
      .sort((a, b) => b.numero - a.numero);
  });

  protected readonly historialVisible = computed(() => {
    const dispositivoId = this.dispositivoSeleccionadoId();
    return this.comparaciones().filter(
      (comparacion) => !dispositivoId || comparacion.dispositivoId === dispositivoId,
    );
  });

  ngOnInit(): void {
    this.cargarDatos();
  }

  protected cambiarDispositivo(): void {
    this.dispositivoSeleccionadoId.set(this.formulario.controls.dispositivoId.value);
    this.resultado.set(null);
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
    this.seleccionarVersionesPredeterminadas();
  }

  protected comparar(): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.mensajeError.set('Selecciona un dispositivo y dos versiones válidas.');
      return;
    }

    const valores = this.formulario.getRawValue();
    if (valores.versionOrigenId === valores.versionDestinoId) {
      this.mensajeError.set('La versión de origen y la versión de destino deben ser diferentes.');
      return;
    }

    this.comparando.set(true);
    this.servicioComparaciones
      .comparar(valores.versionOrigenId, valores.versionDestinoId)
      .pipe(finalize(() => this.comparando.set(false)))
      .subscribe({
        next: (comparacion) => {
          this.resultado.set(comparacion);
          this.comparaciones.update((historial) => [comparacion, ...historial]);
          this.mensajeExito.set(
            comparacion.diferencias.length
              ? `Comparación completada: se identificaron ${comparacion.diferencias.length} diferencias.`
              : 'Comparación completada: las versiones tienen el mismo contenido.',
          );
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible comparar las versiones seleccionadas.'),
          ),
      });
  }

  protected abrirComparacion(comparacionId: number): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
    this.cargandoDetalle.set(true);
    this.servicioComparaciones
      .obtener(comparacionId)
      .pipe(finalize(() => this.cargandoDetalle.set(false)))
      .subscribe({
        next: (comparacion) => this.resultado.set(comparacion),
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible consultar la comparación.'),
          ),
      });
  }

  protected cerrarResultado(): void {
    if (!this.cargandoDetalle()) {
      this.resultado.set(null);
    }
  }

  protected etiquetaTipo(tipo: TipoDiferencia): string {
    const etiquetas: Record<TipoDiferencia, string> = {
      Agregada: 'Agregada',
      Eliminada: 'Eliminada',
      Modificada: 'Modificada',
    };
    return etiquetas[tipo];
  }

  protected totalCambios(comparacion: ComparacionResumen): number {
    return comparacion.agregadas + comparacion.eliminadas + comparacion.modificadas;
  }

  private cargarDatos(): void {
    this.cargando.set(true);
    this.mensajeError.set(null);
    forkJoin({
      versiones: this.servicioCapturas.listarVersiones(),
      comparaciones: this.servicioComparaciones.listar(),
    })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ({ versiones, comparaciones }) => {
          this.versiones.set(versiones);
          this.comparaciones.set(comparaciones);
          const primerDispositivo = this.dispositivosComparables()[0];
          if (primerDispositivo) {
            this.formulario.controls.dispositivoId.setValue(primerDispositivo.id);
            this.dispositivoSeleccionadoId.set(primerDispositivo.id);
            this.seleccionarVersionesPredeterminadas();
          }
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cargar las versiones y comparaciones.'),
          ),
      });
  }

  private seleccionarVersionesPredeterminadas(): void {
    const versiones = this.versionesDispositivo();
    this.formulario.patchValue({
      versionOrigenId: versiones[1]?.id ?? 0,
      versionDestinoId: versiones[0]?.id ?? 0,
    });
  }
}
