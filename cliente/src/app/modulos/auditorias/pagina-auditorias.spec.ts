import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CatalogoFiltrosAuditoria, ResultadoPaginaAuditorias } from './modelos-auditorias';
import { PaginaAuditorias as ComponentePaginaAuditorias } from './pagina-auditorias';
import { ServicioAuditorias } from './servicio-auditorias';

describe('PaginaAuditorias', () => {
  const catalogo: CatalogoFiltrosAuditoria = {
    usuarios: [
      { id: 1, nombreUsuario: 'administrador.principal' },
      { id: 2, nombreUsuario: 'tecnico.prueba' },
    ],
    acciones: ['CrearUsuario', 'ProcesarEventoSyslog'],
    entidades: ['EventoCambio', 'Usuario'],
  };

  const pagina: ResultadoPaginaAuditorias = {
    registros: [
      {
        id: 15,
        usuarioId: 1,
        nombreUsuario: 'administrador.principal',
        accion: 'CrearUsuario',
        entidad: 'Usuario',
        entidadId: 8,
        fecha: '2026-09-23T03:30:00Z',
        estado: 'Exitoso',
        detalle: 'Se creó un usuario técnico.',
      },
      {
        id: 14,
        usuarioId: null,
        nombreUsuario: null,
        accion: 'ProcesarEventoSyslog',
        entidad: 'EventoCambio',
        entidadId: 5,
        fecha: '2026-09-23T03:20:00Z',
        estado: 'Fallido',
        detalle: 'No fue posible completar la captura.',
      },
    ],
    pagina: 1,
    tamanoPagina: 25,
    totalRegistros: 2,
    totalPaginas: 1,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ComponentePaginaAuditorias],
      providers: [
        {
          provide: ServicioAuditorias,
          useValue: {
            obtenerCatalogoFiltros: vi.fn(() => of(catalogo)),
            listar: vi.fn(() => of(pagina)),
          },
        },
      ],
    });
  });

  it('debe presentar los registros y diferenciar sus estados', () => {
    const fixture = TestBed.createComponent(ComponentePaginaAuditorias);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.textContent).toContain('administrador.principal');
    expect(elemento.textContent).toContain('Proceso del sistema');
    expect(elemento.textContent).toContain('CrearUsuario');
    expect(elemento.querySelector('[data-estado="Fallido"]')).not.toBeNull();
    expect(elemento.textContent).toContain('Crear Usuario');
    expect(elemento.textContent).toContain('tecnico.prueba');
  });

  it('debe enviar los filtros y reiniciar la consulta en la primera página', () => {
    const servicio = TestBed.inject(ServicioAuditorias);
    const listar = vi.spyOn(servicio, 'listar');
    const fixture = TestBed.createComponent(ComponentePaginaAuditorias);
    fixture.detectChanges();
    listar.mockClear();

    const componente = fixture.componentInstance as unknown as {
      formulario: { patchValue(valor: object): void };
      aplicarFiltros(): void;
    };
    componente.formulario.patchValue({
      usuarioId: '1',
      accion: 'CrearUsuario',
      entidad: 'Usuario',
      estado: 'Exitoso',
    });
    componente.aplicarFiltros();

    expect(listar).toHaveBeenCalledWith(
      expect.objectContaining({
        pagina: 1,
        usuarioId: 1,
        accion: 'CrearUsuario',
        entidad: 'Usuario',
        estado: 'Exitoso',
      }),
    );
  });
});
