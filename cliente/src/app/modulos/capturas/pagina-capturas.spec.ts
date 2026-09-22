import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { ServicioDispositivos } from '../dispositivos/servicio-dispositivos';
import {
  CapturaResumen,
  EventoCambioResumen,
  VersionConfiguracionResumen,
} from './modelos-capturas';
import { PaginaCapturas } from './pagina-capturas';
import { ServicioCapturas } from './servicio-capturas';

describe('PaginaCapturas', () => {
  const captura: CapturaResumen = {
    id: 4,
    dispositivoId: 1,
    dispositivo: 'nucleo-principal',
    usuarioSolicitante: 'tecnico.pruebas',
    disparador: 'BajoDemanda',
    medio: 'Archivo',
    estado: 'Completada',
    fecha: '2026-09-05T00:00:00Z',
    error: null,
    versionId: 8,
  };
  const version: VersionConfiguracionResumen = {
    id: 8,
    dispositivoId: 1,
    dispositivo: 'nucleo-principal',
    capturaId: 4,
    numero: 2,
    origen: 'Archivo',
    hash: 'a'.repeat(64),
    capturadaEn: '2026-09-05T00:00:00Z',
    comentario: 'Archivo: nucleo.cfg.',
    estado: 'Activa',
    estable: false,
    usuarioSolicitante: 'tecnico.pruebas',
  };
  const evento: EventoCambioResumen = {
    id: 6,
    dispositivoId: 1,
    dispositivo: 'nucleo-principal',
    fuente: '192.168.200.2',
    tipo: 'Syslog',
    fecha: '2026-09-22T03:00:00Z',
    huella: 'b'.repeat(64),
    estado: 'Procesado',
    resumen: '%SYS-5-CONFIG_I: Configured from console',
    capturaId: 4,
    versionId: 8,
  };
  const eventoSinCambios: EventoCambioResumen = {
    ...evento,
    id: 7,
    huella: 'c'.repeat(64),
    estado: 'SinCambios',
    resumen: '%SYS-5-CONFIG_I: Configured from console',
    capturaId: 5,
    versionId: null,
  };

  it('debe mostrar dispositivos autorizados y el historial de versiones', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaCapturas);
    fixture.detectChanges();

    const texto = fixture.nativeElement.textContent as string;
    expect(texto).toContain('Cargar configuración');
    expect(texto).toContain('nucleo-principal');
    expect(texto).toContain('v2');
    expect(texto).toContain('tecnico.pruebas');
  });

  it('debe excluir de la carga los dispositivos no autorizados', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaCapturas);
    fixture.detectChanges();

    const opciones = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('form option'),
    ).map((opcion) => opcion.textContent);
    expect(opciones.join(' ')).toContain('nucleo-principal');
    expect(opciones.join(' ')).not.toContain('borde-pendiente');
  });

  it('debe mostrar las capturas al cambiar la vista', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaCapturas);
    fixture.detectChanges();
    const botonCapturas = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('[role="tab"]'),
    ).find((boton) => boton.textContent?.includes('Capturas'))!;

    botonCapturas.click();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('#4');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Completada');
  });

  it('debe mostrar los eventos Syslog y su versión asociada', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaCapturas);
    fixture.detectChanges();
    const botonEventos = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('[role="tab"]'),
    ).find((boton) => boton.textContent?.includes('Eventos'))!;

    botonEventos.click();
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('Syslog');
    expect(texto).toContain('Procesado');
    expect(texto).toContain('Configured from console');
    expect(texto).toContain('Sin cambios detectados');
  });

  it('debe registrar una captura SSH desde un dispositivo preparado', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaCapturas);
    fixture.detectChanges();
    const boton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="capturar-remotamente"]',
    )!;

    boton.click();
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('La captura SSH creó la versión 3');
    expect(texto).toContain('v3');
  });

  it('debe permitir una captura NETCONF desde un dispositivo preparado', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(PaginaCapturas);
    fixture.detectChanges();
    const formularioRemoto = (fixture.nativeElement as HTMLElement).querySelector(
      '.panel--remota form',
    )!;
    const selector = formularioRemoto.querySelector<HTMLSelectElement>('select')!;
    const opcionNetconf = Array.from(selector.options).find((opcion) =>
      opcion.textContent?.includes('netconf-listo'),
    )!;

    selector.value = opcionNetconf.value;
    selector.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    formularioRemoto
      .querySelector<HTMLButtonElement>('[data-testid="capturar-remotamente"]')!
      .click();
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('La captura NETCONF creó la versión 3');
    expect(texto).toContain('solicitará únicamente la configuración');
  });

  function configurarPrueba(): void {
    TestBed.configureTestingModule({
      imports: [PaginaCapturas],
      providers: [
        provideHttpClient(),
        {
          provide: ServicioDispositivos,
          useValue: {
            listar: () =>
              of([
                {
                  id: 1,
                  nombre: 'nucleo-principal',
                  host: 'core.ejemplo.local',
                  tipo: 'Router',
                  modelo: null,
                  protocolo: 'Ssh',
                  puerto: 22,
                  fuenteEventos: 'Syslog',
                  estado: 'Autorizado',
                  accesoRemotoConfigurado: true,
                },
                {
                  id: 2,
                  nombre: 'borde-pendiente',
                  host: 'borde.ejemplo.local',
                  tipo: 'Router',
                  modelo: null,
                  protocolo: 'Netconf',
                  puerto: 830,
                  fuenteEventos: null,
                  estado: 'NoAutorizado',
                  accesoRemotoConfigurado: false,
                },
                {
                  id: 3,
                  nombre: 'netconf-listo',
                  host: 'netconf.ejemplo.local',
                  tipo: 'Router',
                  modelo: 'IOS XE',
                  protocolo: 'Netconf',
                  puerto: 830,
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
            listarCapturas: () => of([captura]),
            listarVersiones: () => of([version]),
            listarEventos: () => of([eventoSinCambios, evento]),
            obtenerVersion: () => of({ ...version, contenido: 'hostname nucleo' }),
            capturarRemotamente: (dispositivoId: number) =>
              of({
                captura: {
                  ...captura,
                  id: 5,
                  medio: 'Ssh',
                  versionId: 9,
                },
                version: {
                  ...version,
                  id: 9,
                  capturaId: 5,
                  numero: 3,
                  origen: dispositivoId === 3 ? 'CapturaNetconf' : 'CapturaSsh',
                },
              }),
          },
        },
      ],
    });
  }
});
