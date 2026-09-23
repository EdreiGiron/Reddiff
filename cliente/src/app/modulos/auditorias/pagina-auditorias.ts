import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import {
  CatalogoFiltrosAuditoria,
  EstadoAuditoria,
  ResultadoPaginaAuditorias,
} from './modelos-auditorias';
import { ServicioAuditorias } from './servicio-auditorias';

@Component({
  selector: 'rd-pagina-auditorias',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './pagina-auditorias.html',
  styleUrl: './pagina-auditorias.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaAuditorias implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioAuditorias = inject(ServicioAuditorias);

  protected readonly cargando = signal(true);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly catalogoFiltros = signal<CatalogoFiltrosAuditoria>({
    usuarios: [],
    acciones: [],
    entidades: [],
  });
  protected readonly resultado = signal<ResultadoPaginaAuditorias>({
    registros: [],
    pagina: 1,
    tamanoPagina: 25,
    totalRegistros: 0,
    totalPaginas: 0,
  });
  protected readonly paginaActual = computed(() => this.resultado().pagina);
  protected readonly puedeRetroceder = computed(() => this.paginaActual() > 1);
  protected readonly puedeAvanzar = computed(
    () => this.paginaActual() < this.resultado().totalPaginas,
  );

  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    usuarioId: [''],
    accion: [''],
    entidad: [''],
    estado: [''],
    desde: [''],
    hasta: [''],
  });

  ngOnInit(): void {
    this.cargarCatalogoFiltros();
  }

  protected aplicarFiltros(): void {
    this.cargarPagina(1);
  }

  protected limpiarFiltros(): void {
    this.formulario.reset({
      usuarioId: '',
      accion: '',
      entidad: '',
      estado: '',
      desde: '',
      hasta: '',
    });
    this.cargarPagina(1);
  }

  protected cambiarPagina(desplazamiento: number): void {
    const pagina = this.paginaActual() + desplazamiento;
    if (pagina < 1 || pagina > this.resultado().totalPaginas) {
      return;
    }

    this.cargarPagina(pagina);
  }

  protected reintentar(): void {
    if (!this.catalogoFiltros().usuarios.length) {
      this.cargarCatalogoFiltros();
      return;
    }

    this.cargarPagina(this.paginaActual());
  }

  protected formatearEtiqueta(valor: string): string {
    return valor.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  }

  private cargarCatalogoFiltros(): void {
    this.cargando.set(true);
    this.mensajeError.set(null);
    this.servicioAuditorias.obtenerCatalogoFiltros().subscribe({
      next: (catalogo) => {
        this.catalogoFiltros.set(catalogo);
        this.cargarPagina(1);
      },
      error: (error: unknown) => {
        this.cargando.set(false);
        this.mensajeError.set(
          obtenerMensajeError(error, 'No fue posible preparar los filtros de auditoría.'),
        );
      },
    });
  }

  private cargarPagina(pagina: number): void {
    const valores = this.formulario.getRawValue();
    const usuarioId = Number.parseInt(valores.usuarioId, 10);
    this.cargando.set(true);
    this.mensajeError.set(null);

    this.servicioAuditorias
      .listar({
        pagina,
        tamanoPagina: 25,
        usuarioId: Number.isInteger(usuarioId) && usuarioId > 0 ? usuarioId : undefined,
        accion: valores.accion.trim() || undefined,
        entidad: valores.entidad.trim() || undefined,
        estado: (valores.estado || undefined) as EstadoAuditoria | undefined,
        desde: this.convertirFecha(valores.desde, false),
        hasta: this.convertirFecha(valores.hasta, true),
      })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (resultado) => this.resultado.set(resultado),
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible consultar la bitácora de auditoría.'),
          ),
      });
  }

  private convertirFecha(valor: string, finDelDia: boolean): string | undefined {
    if (!valor) {
      return undefined;
    }

    const fecha = new Date(`${valor}T${finDelDia ? '23:59:59.999' : '00:00:00.000'}`);
    return Number.isNaN(fecha.getTime()) ? undefined : fecha.toISOString();
  }
}
