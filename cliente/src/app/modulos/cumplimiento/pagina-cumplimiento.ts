import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { VersionConfiguracionResumen } from '../capturas/modelos-capturas';
import { ServicioCapturas } from '../capturas/servicio-capturas';
import { DispositivoResumen } from '../dispositivos/modelos-dispositivos';
import { ServicioDispositivos } from '../dispositivos/servicio-dispositivos';
import {
  AlcanceBaseline,
  BaselineResumen,
  CriterioReglaBaseline,
  CrearBaselineSolicitud,
  ResultadoGeneralVerificacion,
  VerificacionDetalle,
  VerificacionResumen,
} from './modelos-cumplimiento';
import { ServicioCumplimiento } from './servicio-cumplimiento';

type FormularioRegla = FormGroup<{
  criterio: FormControl<CriterioReglaBaseline>;
  esperado: FormControl<string>;
  obligatoria: FormControl<boolean>;
}>;

@Component({
  selector: 'rd-pagina-cumplimiento',
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  templateUrl: './pagina-cumplimiento.html',
  styleUrl: './pagina-cumplimiento.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaCumplimiento implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioCumplimiento = inject(ServicioCumplimiento);
  private readonly servicioDispositivos = inject(ServicioDispositivos);
  private readonly servicioCapturas = inject(ServicioCapturas);
  protected readonly servicioSesion = inject(ServicioSesion);

  protected readonly baselines = signal<BaselineResumen[]>([]);
  protected readonly verificaciones = signal<VerificacionResumen[]>([]);
  protected readonly dispositivos = signal<DispositivoResumen[]>([]);
  protected readonly versiones = signal<VersionConfiguracionResumen[]>([]);
  protected readonly baselineSeleccionadaId = signal(0);
  protected readonly resultado = signal<VerificacionDetalle | null>(null);
  protected readonly cargando = signal(true);
  protected readonly guardandoBaseline = signal(false);
  protected readonly verificando = signal(false);
  protected readonly cambiandoEstadoId = signal<number | null>(null);
  protected readonly cargandoDetalle = signal(false);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);

  protected readonly formularioBaseline = this.constructorFormulario.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    alcance: ['Dispositivo' as AlcanceBaseline, Validators.required],
    dispositivoId: [0],
    tipoDispositivo: [''],
    versionReferenciaId: [0],
    reglas: this.constructorFormulario.nonNullable.array<FormularioRegla>([this.crearGrupoRegla()]),
  });

  protected readonly formularioVerificacion = this.constructorFormulario.nonNullable.group({
    baselineId: [0, [Validators.required, Validators.min(1)]],
    versionId: [0, [Validators.required, Validators.min(1)]],
  });

  protected readonly tiposDispositivo = computed(() =>
    [...new Set(this.dispositivos().map((dispositivo) => dispositivo.tipo))].sort((a, b) =>
      a.localeCompare(b, 'es'),
    ),
  );

  protected readonly baselinesActivas = computed(() =>
    this.baselines().filter((baseline) => baseline.activa),
  );

  protected readonly baselineSeleccionada = computed(
    () =>
      this.baselines().find((baseline) => baseline.id === this.baselineSeleccionadaId()) ?? null,
  );

  protected readonly versionesCompatibles = computed(() => {
    const baseline = this.baselineSeleccionada();
    if (!baseline) {
      return [];
    }

    return this.versiones()
      .filter((version) => this.versionCompatible(version, baseline))
      .sort((a, b) => b.numero - a.numero);
  });

  protected readonly historialVisible = computed(() => {
    const baselineId = this.baselineSeleccionadaId();
    return this.verificaciones().filter(
      (verificacion) => !baselineId || verificacion.baselineId === baselineId,
    );
  });

  protected get reglasFormulario() {
    return this.formularioBaseline.controls.reglas;
  }

  ngOnInit(): void {
    this.cargarDatos();
  }

  protected agregarRegla(): void {
    if (this.reglasFormulario.length < 100) {
      this.reglasFormulario.push(this.crearGrupoRegla());
    }
  }

  protected quitarRegla(indice: number): void {
    if (this.reglasFormulario.length > 1) {
      this.reglasFormulario.removeAt(indice);
    }
  }

  protected cambiarAlcanceCreacion(): void {
    const alcance = this.formularioBaseline.controls.alcance.value;
    const primerDispositivo = this.dispositivos()[0];
    const primerTipo = this.tiposDispositivo()[0] ?? '';
    this.formularioBaseline.patchValue({
      dispositivoId: alcance === 'Dispositivo' ? (primerDispositivo?.id ?? 0) : 0,
      tipoDispositivo: alcance === 'TipoDispositivo' ? primerTipo : '',
      versionReferenciaId: 0,
    });
  }

  protected cambiarObjetivoCreacion(): void {
    this.formularioBaseline.controls.versionReferenciaId.setValue(0);
  }

  protected versionesReferenciaDisponibles(): VersionConfiguracionResumen[] {
    const valores = this.formularioBaseline.getRawValue();
    return this.versiones()
      .filter((version) => {
        if (valores.alcance === 'Dispositivo') {
          return version.dispositivoId === valores.dispositivoId;
        }

        const dispositivo = this.dispositivos().find((item) => item.id === version.dispositivoId);
        return dispositivo?.tipo === valores.tipoDispositivo;
      })
      .sort((a, b) => b.numero - a.numero);
  }

  protected crearBaseline(): void {
    this.limpiarMensajes();
    if (this.formularioBaseline.invalid) {
      this.formularioBaseline.markAllAsTouched();
      this.mensajeError.set('Completa el nombre y todas las reglas de la línea base.');
      return;
    }

    const valores = this.formularioBaseline.getRawValue();
    if (valores.alcance === 'Dispositivo' && valores.dispositivoId <= 0) {
      this.mensajeError.set('Selecciona un dispositivo para la línea base.');
      return;
    }

    if (valores.alcance === 'TipoDispositivo' && !valores.tipoDispositivo) {
      this.mensajeError.set('Selecciona un tipo de dispositivo para la línea base.');
      return;
    }

    const solicitud: CrearBaselineSolicitud = {
      nombre: valores.nombre,
      alcance: valores.alcance,
      dispositivoId: valores.alcance === 'Dispositivo' ? valores.dispositivoId : null,
      tipoDispositivo: valores.alcance === 'TipoDispositivo' ? valores.tipoDispositivo : null,
      versionReferenciaId: valores.versionReferenciaId || null,
      reglas: valores.reglas,
    };

    this.guardandoBaseline.set(true);
    this.servicioCumplimiento
      .crearBaseline(solicitud)
      .pipe(finalize(() => this.guardandoBaseline.set(false)))
      .subscribe({
        next: (baseline) => {
          this.baselines.update((actuales) => [baseline, ...actuales]);
          this.seleccionarBaseline(baseline.id);
          this.restablecerFormularioBaseline();
          this.mensajeExito.set(
            `Línea base creada con ${baseline.reglas.length} reglas controladas.`,
          );
        },
        error: (error: unknown) =>
          this.mensajeError.set(obtenerMensajeError(error, 'No fue posible crear la línea base.')),
      });
  }

  protected cambiarBaseline(): void {
    this.seleccionarBaseline(this.formularioVerificacion.controls.baselineId.value);
  }

  protected ejecutarVerificacion(): void {
    this.limpiarMensajes();
    if (this.formularioVerificacion.invalid) {
      this.formularioVerificacion.markAllAsTouched();
      this.mensajeError.set('Selecciona una línea base activa y una versión compatible.');
      return;
    }

    const valores = this.formularioVerificacion.getRawValue();
    this.verificando.set(true);
    this.servicioCumplimiento
      .verificar(valores.baselineId, valores.versionId)
      .pipe(finalize(() => this.verificando.set(false)))
      .subscribe({
        next: (verificacion) => {
          this.resultado.set(verificacion);
          this.verificaciones.update((historial) => [verificacion, ...historial]);
          this.mensajeExito.set(
            `Verificación completada con resultado: ${this.etiquetaResultado(verificacion.resultadoGeneral)}.`,
          );
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible ejecutar la verificación.'),
          ),
      });
  }

  protected cambiarEstado(baseline: BaselineResumen): void {
    this.limpiarMensajes();
    this.cambiandoEstadoId.set(baseline.id);
    this.servicioCumplimiento
      .cambiarEstadoBaseline(baseline.id, !baseline.activa)
      .pipe(finalize(() => this.cambiandoEstadoId.set(null)))
      .subscribe({
        next: (actualizada) => {
          this.baselines.update((baselines) =>
            baselines.map((item) => (item.id === actualizada.id ? actualizada : item)),
          );
          if (!actualizada.activa && this.baselineSeleccionadaId() === actualizada.id) {
            this.seleccionarBaseline(this.baselinesActivas()[0]?.id ?? 0);
          }
          this.mensajeExito.set(
            actualizada.activa ? 'La línea base fue activada.' : 'La línea base fue desactivada.',
          );
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cambiar el estado de la línea base.'),
          ),
      });
  }

  protected abrirVerificacion(verificacionId: number): void {
    this.limpiarMensajes();
    this.cargandoDetalle.set(true);
    this.servicioCumplimiento
      .obtenerVerificacion(verificacionId)
      .pipe(finalize(() => this.cargandoDetalle.set(false)))
      .subscribe({
        next: (verificacion) => this.resultado.set(verificacion),
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible consultar la verificación.'),
          ),
      });
  }

  protected cerrarResultado(): void {
    if (!this.cargandoDetalle()) {
      this.resultado.set(null);
    }
  }

  protected descripcionAlcance(baseline: BaselineResumen): string {
    return baseline.alcance === 'Dispositivo'
      ? (baseline.dispositivo ?? 'Dispositivo no disponible')
      : `Tipo ${baseline.tipoDispositivo}`;
  }

  protected etiquetaCriterio(criterio: CriterioReglaBaseline): string {
    const etiquetas: Record<CriterioReglaBaseline, string> = {
      Contiene: 'Debe contener',
      NoContiene: 'No debe contener',
      CoincideExpresionRegular: 'Expresión regular',
    };
    return etiquetas[criterio];
  }

  protected contarObligatorias(baseline: BaselineResumen): number {
    return baseline.reglas.filter((regla) => regla.obligatoria).length;
  }

  protected etiquetaResultado(resultado: ResultadoGeneralVerificacion): string {
    const etiquetas: Record<ResultadoGeneralVerificacion, string> = {
      Cumple: 'Cumple',
      Incumple: 'Requiere atención',
      NoEvaluable: 'No evaluable',
    };
    return etiquetas[resultado];
  }

  private cargarDatos(): void {
    this.cargando.set(true);
    this.mensajeError.set(null);
    forkJoin({
      baselines: this.servicioCumplimiento.listarBaselines(),
      verificaciones: this.servicioCumplimiento.listarVerificaciones(),
      dispositivos: this.servicioDispositivos.listar(),
      versiones: this.servicioCapturas.listarVersiones(),
    })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ({ baselines, verificaciones, dispositivos, versiones }) => {
          this.baselines.set(baselines);
          this.verificaciones.set(verificaciones);
          this.dispositivos.set(dispositivos);
          this.versiones.set(versiones);
          this.restablecerFormularioBaseline();
          this.seleccionarBaseline(this.baselinesActivas()[0]?.id ?? 0);
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cargar la información de cumplimiento.'),
          ),
      });
  }

  private seleccionarBaseline(baselineId: number): void {
    this.baselineSeleccionadaId.set(baselineId);
    this.formularioVerificacion.controls.baselineId.setValue(baselineId);
    this.resultado.set(null);
    const versiones = this.versionesCompatibles();
    this.formularioVerificacion.controls.versionId.setValue(versiones[0]?.id ?? 0);
  }

  private restablecerFormularioBaseline(): void {
    this.formularioBaseline.reset({
      nombre: '',
      alcance: 'Dispositivo',
      dispositivoId: this.dispositivos()[0]?.id ?? 0,
      tipoDispositivo: '',
      versionReferenciaId: 0,
    });
    this.reglasFormulario.clear();
    this.reglasFormulario.push(this.crearGrupoRegla());
  }

  private crearGrupoRegla(): FormularioRegla {
    return this.constructorFormulario.nonNullable.group({
      criterio: new FormControl<CriterioReglaBaseline>('Contiene', { nonNullable: true }),
      esperado: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(10_000)],
      }),
      obligatoria: new FormControl(true, { nonNullable: true }),
    });
  }

  private versionCompatible(
    version: VersionConfiguracionResumen,
    baseline: BaselineResumen,
  ): boolean {
    if (baseline.alcance === 'Dispositivo') {
      return version.dispositivoId === baseline.dispositivoId;
    }

    const dispositivo = this.dispositivos().find((item) => item.id === version.dispositivoId);
    return dispositivo?.tipo.toLocaleLowerCase() === baseline.tipoDispositivo?.toLocaleLowerCase();
  }

  private limpiarMensajes(): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
  }
}
