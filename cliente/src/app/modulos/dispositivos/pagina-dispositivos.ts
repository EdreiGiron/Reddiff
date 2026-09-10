import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import {
  AccesoRemotoResumen,
  DispositivoResumen,
  EstadoDispositivo,
  FuenteEventosDispositivo,
  GuardarDispositivoSolicitud,
  ProtocoloDispositivo,
} from './modelos-dispositivos';
import { FormularioAccesoRemoto } from './acceso-remoto/formulario-acceso-remoto';
import { ServicioDispositivos } from './servicio-dispositivos';

interface CambioEstadoPendiente {
  dispositivo: DispositivoResumen;
  estado: EstadoDispositivo;
}

@Component({
  selector: 'rd-pagina-dispositivos',
  imports: [ReactiveFormsModule, FormularioAccesoRemoto],
  templateUrl: './pagina-dispositivos.html',
  styleUrl: './pagina-dispositivos.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaDispositivos implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioDispositivos = inject(ServicioDispositivos);
  protected readonly servicioSesion = inject(ServicioSesion);

  protected readonly dispositivos = signal<DispositivoResumen[]>([]);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly cambiandoEstado = signal(false);
  protected readonly revocandoAcceso = signal(false);
  protected readonly formularioVisible = signal(false);
  protected readonly dispositivoEditado = signal<DispositivoResumen | null>(null);
  protected readonly dispositivoAcceso = signal<DispositivoResumen | null>(null);
  protected readonly revocacionAccesoPendiente = signal<DispositivoResumen | null>(null);
  protected readonly cambioPendiente = signal<CambioEstadoPendiente | null>(null);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);
  protected readonly busqueda = signal('');
  protected readonly filtroEstado = signal<EstadoDispositivo | 'Todos'>('Todos');

  protected readonly protocolos: ReadonlyArray<{
    valor: ProtocoloDispositivo;
    etiqueta: string;
    puerto: number;
  }> = [
    { valor: 'Ssh', etiqueta: 'SSH', puerto: 22 },
    { valor: 'Netconf', etiqueta: 'NETCONF', puerto: 830 },
  ];

  protected readonly fuentesEventos: ReadonlyArray<{
    valor: FuenteEventosDispositivo;
    etiqueta: string;
  }> = [
    { valor: 'SnmpTrap', etiqueta: 'SNMP trap' },
    { valor: 'SnmpInform', etiqueta: 'SNMP inform' },
    { valor: 'Syslog', etiqueta: 'Syslog' },
  ];

  protected readonly dispositivosFiltrados = computed(() => {
    const criterio = this.busqueda().trim().toLocaleLowerCase('es');
    const estado = this.filtroEstado();

    return this.dispositivos().filter((dispositivo) => {
      const coincideEstado = estado === 'Todos' || dispositivo.estado === estado;
      const coincideTexto =
        !criterio ||
        [dispositivo.nombre, dispositivo.host, dispositivo.tipo, dispositivo.modelo ?? ''].some(
          (valor) => valor.toLocaleLowerCase('es').includes(criterio),
        );
      return coincideEstado && coincideTexto;
    });
  });

  protected readonly totalAutorizados = computed(
    () => this.dispositivos().filter((dispositivo) => dispositivo.estado === 'Autorizado').length,
  );

  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    host: ['', [Validators.required, Validators.maxLength(255)]],
    tipo: ['', [Validators.required, Validators.maxLength(100)]],
    modelo: ['', [Validators.maxLength(120)]],
    protocolo: ['Ssh' as ProtocoloDispositivo, [Validators.required]],
    puerto: [22, [Validators.required, Validators.min(1), Validators.max(65535)]],
    fuenteEventos: ['' as FuenteEventosDispositivo | ''],
  });

  ngOnInit(): void {
    this.cargarDispositivos();
  }

  protected abrirNuevo(): void {
    this.limpiarMensajes();
    this.dispositivoEditado.set(null);
    this.formulario.reset({
      nombre: '',
      host: '',
      tipo: '',
      modelo: '',
      protocolo: 'Ssh',
      puerto: 22,
      fuenteEventos: '',
    });
    this.formularioVisible.set(true);
  }

  protected abrirEdicion(dispositivo: DispositivoResumen): void {
    this.limpiarMensajes();
    this.dispositivoEditado.set(dispositivo);
    this.formulario.reset({
      nombre: dispositivo.nombre,
      host: dispositivo.host,
      tipo: dispositivo.tipo,
      modelo: dispositivo.modelo ?? '',
      protocolo: dispositivo.protocolo,
      puerto: dispositivo.puerto,
      fuenteEventos: dispositivo.fuenteEventos ?? '',
    });
    this.formularioVisible.set(true);
  }

  protected cerrarFormulario(): void {
    if (!this.guardando()) {
      this.formularioVisible.set(false);
      this.dispositivoEditado.set(null);
      this.mensajeError.set(null);
    }
  }

  protected alCambiarProtocolo(): void {
    const puertoActual = this.formulario.controls.puerto.value;
    if (puertoActual !== 22 && puertoActual !== 830) {
      return;
    }

    const opcion = this.protocolos.find(
      (protocolo) => protocolo.valor === this.formulario.controls.protocolo.value,
    );
    if (opcion) {
      this.formulario.controls.puerto.setValue(opcion.puerto);
    }
  }

  protected guardar(): void {
    this.mensajeError.set(null);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();
    const solicitud: GuardarDispositivoSolicitud = {
      nombre: valores.nombre.trim(),
      host: valores.host.trim(),
      tipo: valores.tipo.trim(),
      modelo: valores.modelo.trim() || null,
      protocolo: valores.protocolo,
      puerto: valores.puerto,
      fuenteEventos: valores.fuenteEventos || null,
    };
    const editado = this.dispositivoEditado();
    const operacion = editado
      ? this.servicioDispositivos.actualizar(editado.id, solicitud)
      : this.servicioDispositivos.crear(solicitud);

    this.guardando.set(true);
    operacion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: (dispositivo) => {
        this.actualizarLista(dispositivo);
        this.formularioVisible.set(false);
        this.dispositivoEditado.set(null);
        this.mensajeExito.set(
          editado
            ? 'El dispositivo fue actualizado correctamente.'
            : 'El dispositivo fue registrado sin autorización.',
        );
      },
      error: (error: unknown) =>
        this.mensajeError.set(obtenerMensajeError(error, 'No fue posible guardar el dispositivo.')),
    });
  }

  protected solicitarCambio(dispositivo: DispositivoResumen, estado: EstadoDispositivo): void {
    this.limpiarMensajes();
    this.cambioPendiente.set({ dispositivo, estado });
  }

  protected cancelarCambio(): void {
    if (!this.cambiandoEstado()) {
      this.cambioPendiente.set(null);
    }
  }

  protected confirmarCambio(): void {
    const pendiente = this.cambioPendiente();
    if (!pendiente) {
      return;
    }

    this.cambiandoEstado.set(true);
    this.servicioDispositivos
      .cambiarEstado(pendiente.dispositivo.id, pendiente.estado)
      .pipe(finalize(() => this.cambiandoEstado.set(false)))
      .subscribe({
        next: (dispositivo) => {
          this.actualizarLista(dispositivo);
          this.cambioPendiente.set(null);
          this.mensajeExito.set(
            `El dispositivo cambió a ${this.etiquetaEstado(dispositivo.estado)}.`,
          );
        },
        error: (error: unknown) => {
          this.cambioPendiente.set(null);
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cambiar el estado del dispositivo.'),
          );
        },
      });
  }

  protected abrirAccesoRemoto(dispositivo: DispositivoResumen): void {
    this.limpiarMensajes();
    this.dispositivoAcceso.set(dispositivo);
  }

  protected cerrarAccesoRemoto(): void {
    this.dispositivoAcceso.set(null);
    this.mensajeError.set(null);
  }

  protected accesoRemotoActualizado(detalle: AccesoRemotoResumen): void {
    const dispositivo = this.dispositivoAcceso();
    if (!dispositivo) {
      return;
    }

    const reemplazo = dispositivo.accesoRemotoConfigurado;
    this.actualizarEstadoAcceso(dispositivo.id, detalle.configurado);
    this.dispositivoAcceso.set(null);
    this.mensajeExito.set(
      reemplazo
        ? 'El acceso remoto fue reemplazado correctamente.'
        : 'El acceso remoto fue configurado correctamente.',
    );
  }

  protected solicitarRevocacionAcceso(dispositivo: DispositivoResumen): void {
    this.limpiarMensajes();
    this.revocacionAccesoPendiente.set(dispositivo);
  }

  protected cancelarRevocacionAcceso(): void {
    if (!this.revocandoAcceso()) {
      this.revocacionAccesoPendiente.set(null);
    }
  }

  protected confirmarRevocacionAcceso(): void {
    const dispositivo = this.revocacionAccesoPendiente();
    if (!dispositivo) {
      return;
    }

    this.revocandoAcceso.set(true);
    this.servicioDispositivos
      .revocarAccesoRemoto(dispositivo.id)
      .pipe(finalize(() => this.revocandoAcceso.set(false)))
      .subscribe({
        next: (detalle) => {
          this.actualizarEstadoAcceso(dispositivo.id, detalle.configurado);
          this.revocacionAccesoPendiente.set(null);
          this.mensajeExito.set('El acceso remoto fue revocado correctamente.');
        },
        error: (error: unknown) => {
          this.revocacionAccesoPendiente.set(null);
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible revocar el acceso remoto.'),
          );
        },
      });
  }

  protected actualizarBusqueda(evento: Event): void {
    this.busqueda.set((evento.target as HTMLInputElement).value);
  }

  protected actualizarFiltroEstado(evento: Event): void {
    this.filtroEstado.set(
      (evento.target as HTMLSelectElement).value as EstadoDispositivo | 'Todos',
    );
  }

  protected reintentarCarga(): void {
    this.cargarDispositivos();
  }

  protected etiquetaProtocolo(protocolo: ProtocoloDispositivo): string {
    return protocolo === 'Ssh' ? 'SSH' : 'NETCONF';
  }

  protected etiquetaFuente(fuente: FuenteEventosDispositivo | null): string {
    return this.fuentesEventos.find((opcion) => opcion.valor === fuente)?.etiqueta ?? 'Sin eventos';
  }

  protected etiquetaEstado(estado: EstadoDispositivo): string {
    const etiquetas: Record<EstadoDispositivo, string> = {
      NoAutorizado: 'No autorizado',
      Autorizado: 'Autorizado',
      Inactivo: 'Inactivo',
    };
    return etiquetas[estado];
  }

  protected tituloCambio(pendiente: CambioEstadoPendiente): string {
    const acciones: Record<EstadoDispositivo, string> = {
      NoAutorizado: pendiente.dispositivo.estado === 'Inactivo' ? 'Reactivar' : 'Revocar acceso a',
      Autorizado: 'Autorizar',
      Inactivo: 'Desactivar',
    };
    return `${acciones[pendiente.estado]} ${pendiente.dispositivo.nombre}`;
  }

  private cargarDispositivos(): void {
    this.cargando.set(true);
    this.limpiarMensajes();
    this.servicioDispositivos
      .listar()
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (dispositivos) => this.dispositivos.set(dispositivos),
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cargar el inventario de dispositivos.'),
          ),
      });
  }

  private actualizarLista(actualizado: DispositivoResumen): void {
    this.dispositivos.update((dispositivos) => {
      const existe = dispositivos.some((dispositivo) => dispositivo.id === actualizado.id);
      const resultado = existe
        ? dispositivos.map((dispositivo) =>
            dispositivo.id === actualizado.id ? actualizado : dispositivo,
          )
        : [...dispositivos, actualizado];
      return resultado.sort((primero, segundo) => primero.nombre.localeCompare(segundo.nombre));
    });
  }

  private actualizarEstadoAcceso(dispositivoId: number, configurado: boolean): void {
    this.dispositivos.update((dispositivos) =>
      dispositivos.map((dispositivo) =>
        dispositivo.id === dispositivoId
          ? { ...dispositivo, accesoRemotoConfigurado: configurado }
          : dispositivo,
      ),
    );
  }

  private limpiarMensajes(): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
  }
}
