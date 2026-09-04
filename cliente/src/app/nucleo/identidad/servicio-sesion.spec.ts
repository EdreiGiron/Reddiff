import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ServicioProteccionCsrf } from '../http/servicio-proteccion-csrf';
import { SesionUsuario } from './modelos-identidad';
import { ServicioSesion } from './servicio-sesion';

describe('ServicioSesion', () => {
  const sesionAdministrador: SesionUsuario = {
    usuarioId: 1,
    nombreUsuario: 'administrador.principal',
    rol: 'Administrador',
    expiraEn: '2026-09-04T05:00:00Z',
  };

  let servicio: ServicioSesion;
  let controladorHttp: HttpTestingController;
  let invalidaciones: number;

  beforeEach(() => {
    invalidaciones = 0;
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ServicioProteccionCsrf,
          useValue: { invalidar: () => invalidaciones++ },
        },
      ],
    });

    servicio = TestBed.inject(ServicioSesion);
    controladorHttp = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controladorHttp.verify());

  it('debe restaurar una sesión existente una sola vez', () => {
    servicio.asegurarSesion().subscribe();

    const solicitud = controladorHttp.expectOne('/api/autenticacion/sesion');
    solicitud.flush(sesionAdministrador);

    expect(servicio.sesion()).toEqual(sesionAdministrador);
    expect(servicio.esAdministrador()).toBe(true);

    servicio.asegurarSesion().subscribe();
    controladorHttp.expectNone('/api/autenticacion/sesion');
  });

  it('debe conservar la sesión solo en memoria después de autenticar', () => {
    servicio
      .iniciarSesion({
        nombreUsuario: 'administrador.principal',
        contrasena: 'una-contrasena-segura',
      })
      .subscribe();

    const solicitud = controladorHttp.expectOne('/api/autenticacion/iniciar-sesion');
    solicitud.flush(sesionAdministrador);

    expect(servicio.sesion()).toEqual(sesionAdministrador);
    expect(servicio.autenticado()).toBe(true);
    expect(invalidaciones).toBe(1);
  });

  it('debe tratar una sesión inexistente como acceso anónimo', () => {
    servicio.asegurarSesion().subscribe();

    const solicitud = controladorHttp.expectOne('/api/autenticacion/sesion');
    solicitud.flush({}, { status: 401, statusText: 'No autorizado' });

    expect(servicio.sesion()).toBeNull();
    expect(servicio.inicializada()).toBe(true);
  });
});
