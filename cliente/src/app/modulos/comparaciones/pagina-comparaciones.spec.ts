import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { VersionConfiguracionResumen } from '../capturas/modelos-capturas';
import { ServicioCapturas } from '../capturas/servicio-capturas';
import { ComparacionDetalle } from './modelos-comparaciones';
import { PaginaComparaciones } from './pagina-comparaciones';
import { ServicioComparaciones } from './servicio-comparaciones';

describe('PaginaComparaciones', () => {
  const versiones: VersionConfiguracionResumen[] = [
    crearVersion(2, 2, '2026-09-18T02:00:00Z'),
    crearVersion(1, 1, '2026-09-18T01:00:00Z'),
  ];
  const comparacion: ComparacionDetalle = {
    id: 7,
    dispositivoId: 1,
    dispositivo: 'RouterGNS3',
    versionOrigen: {
      id: 1,
      numero: 1,
      hash: 'a'.repeat(64),
      capturadaEn: '2026-09-18T01:00:00Z',
      estable: false,
    },
    versionDestino: {
      id: 2,
      numero: 2,
      hash: 'b'.repeat(64),
      capturadaEn: '2026-09-18T02:00:00Z',
      estable: false,
    },
    usuario: 'tecnico.pruebas',
    fecha: '2026-09-18T02:10:00Z',
    estado: 'Completado',
    agregadas: 1,
    eliminadas: 0,
    modificadas: 1,
    diferencias: [
      {
        linea: 3,
        tipo: 'Modificada',
        textoAnterior: ' shutdown',
        textoNuevo: ' no shutdown',
      },
      {
        linea: 4,
        tipo: 'Agregada',
        textoAnterior: null,
        textoNuevo: 'ip cef',
      },
    ],
  };

  it('debe seleccionar las dos versiones más recientes del dispositivo', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaComparaciones);
    fixture.detectChanges();

    const selectores = (fixture.nativeElement as HTMLElement).querySelectorAll('select');
    expect(selectores.length).toBe(3);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('RouterGNS3');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('v1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('v2');
  });

  it('debe mostrar líneas agregadas y modificadas al comparar', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaComparaciones);
    fixture.detectChanges();
    const boton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="comparar-versiones"]',
    )!;

    boton.click();
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('se identificaron 2 diferencias');
    expect(texto).toContain('shutdown');
    expect(texto).toContain('no shutdown');
    expect(texto).toContain('ip cef');
  });

  function configurarPrueba(): void {
    TestBed.configureTestingModule({
      imports: [PaginaComparaciones],
      providers: [
        provideHttpClient(),
        {
          provide: ServicioCapturas,
          useValue: { listarVersiones: () => of(versiones) },
        },
        {
          provide: ServicioComparaciones,
          useValue: {
            listar: () => of([]),
            comparar: () => of(comparacion),
            obtener: () => of(comparacion),
          },
        },
      ],
    });
  }

  function crearVersion(
    id: number,
    numero: number,
    capturadaEn: string,
  ): VersionConfiguracionResumen {
    return {
      id,
      dispositivoId: 1,
      dispositivo: 'RouterGNS3',
      capturaId: id,
      numero,
      origen: 'CapturaSsh',
      hash: `${id}`.repeat(64),
      capturadaEn,
      comentario: null,
      estado: 'Activa',
      estable: false,
      usuarioSolicitante: 'tecnico.pruebas',
    };
  }
});
