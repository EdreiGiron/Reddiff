import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { ServicioCapturas } from '../capturas/servicio-capturas';
import { ServicioDispositivos } from '../dispositivos/servicio-dispositivos';
import { BaselineResumen, VerificacionDetalle } from './modelos-cumplimiento';
import { PaginaCumplimiento } from './pagina-cumplimiento';
import { ServicioCumplimiento } from './servicio-cumplimiento';

describe('PaginaCumplimiento', () => {
  const baseline: BaselineResumen = {
    id: 4,
    nombre: 'Controles del router principal',
    alcance: 'Dispositivo',
    dispositivoId: 10,
    dispositivo: 'RouterGNS3',
    tipoDispositivo: null,
    activa: true,
    versionReferencia: null,
    reglas: [
      {
        id: 7,
        criterio: 'Contiene',
        esperado: 'no ip http server',
        obligatoria: true,
      },
    ],
  };

  const verificacion: VerificacionDetalle = {
    id: 6,
    baselineId: 4,
    baseline: baseline.nombre,
    dispositivoId: 10,
    dispositivo: 'RouterGNS3',
    versionId: 30,
    version: 3,
    usuario: 'tecnico.prueba',
    fecha: '2026-09-18T03:00:00Z',
    estado: 'Completado',
    resultadoGeneral: 'Cumple',
    totalReglas: 1,
    cumplidas: 1,
    incumplidas: 0,
    noEvaluables: 0,
    porcentajeCumplimiento: 100,
    resultados: [
      {
        reglaId: 7,
        criterio: 'Contiene',
        esperado: 'no ip http server',
        obligatoria: true,
        estado: 'Cumplida',
        evidencia: 'El contenido esperado está presente en la versión.',
      },
    ],
  };

  it('debe mostrar al administrador la creación y la verificación', () => {
    const { fixture } = configurarPrueba(true);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.textContent).toContain('Definir una línea base');
    expect(elemento.textContent).toContain('Verificar una versión preservada');
    expect(elemento.querySelector('[data-testid="crear-baseline"]')).not.toBeNull();
  });

  it('debe ocultar al técnico la administración y permitir la verificación', () => {
    const { fixture } = configurarPrueba(false);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.textContent).not.toContain('Definir una línea base');
    expect(elemento.querySelector('[data-testid="crear-baseline"]')).toBeNull();
    expect(elemento.querySelector('[data-testid="verificar-version"]')).not.toBeNull();
  });

  it('debe presentar el resultado de cumplimiento obtenido del servidor', () => {
    const { fixture, verificar } = configurarPrueba(false);
    fixture.detectChanges();

    const boton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="verificar-version"]',
    );
    boton?.click();
    fixture.detectChanges();

    expect(verificar).toHaveBeenCalledWith(4, 30);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('100%');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Cumplida');
  });

  function configurarPrueba(esAdministrador: boolean): {
    fixture: ComponentFixture<PaginaCumplimiento>;
    verificar: ReturnType<typeof vi.fn>;
  } {
    TestBed.resetTestingModule();
    const verificar = vi.fn(() => of(verificacion));
    TestBed.configureTestingModule({
      imports: [PaginaCumplimiento],
      providers: [
        {
          provide: ServicioSesion,
          useValue: {
            sesion: signal({
              usuarioId: 2,
              nombreUsuario: esAdministrador ? 'administrador.principal' : 'tecnico.prueba',
              rol: esAdministrador ? 'Administrador' : 'Tecnico',
              expiraEn: null,
            }),
            esAdministrador: signal(esAdministrador),
          },
        },
        {
          provide: ServicioDispositivos,
          useValue: {
            listar: () =>
              of([
                {
                  id: 10,
                  nombre: 'RouterGNS3',
                  host: '192.168.200.2',
                  tipo: 'Router',
                  modelo: '7200',
                  protocolo: 'Ssh',
                  puerto: 22,
                  fuenteEventos: null,
                  estado: 'Autorizado',
                  accesoRemotoConfigurado: true,
                },
              ]),
          },
        },
        {
          provide: ServicioCapturas,
          useValue: {
            listarVersiones: () =>
              of([
                {
                  id: 30,
                  dispositivoId: 10,
                  dispositivo: 'RouterGNS3',
                  capturaId: 20,
                  numero: 3,
                  origen: 'CapturaSsh',
                  hash: 'a'.repeat(64),
                  capturadaEn: '2026-09-18T02:00:00Z',
                  comentario: null,
                  estado: 'Activa',
                  estable: false,
                  usuarioSolicitante: 'tecnico.prueba',
                },
              ]),
          },
        },
        {
          provide: ServicioCumplimiento,
          useValue: {
            listarBaselines: () => of([baseline]),
            listarVerificaciones: () => of([]),
            crearBaseline: () => of(baseline),
            cambiarEstadoBaseline: () => of({ ...baseline, activa: false }),
            verificar,
            obtenerVerificacion: () => of(verificacion),
          },
        },
      ],
    });

    return { fixture: TestBed.createComponent(PaginaCumplimiento), verificar };
  }
});
