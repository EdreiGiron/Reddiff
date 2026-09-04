import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { interceptorApiSegura } from './interceptor-api-segura';

describe('interceptorApiSegura', () => {
  let http: HttpClient;
  let controladorHttp: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([interceptorApiSegura])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controladorHttp = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controladorHttp.verify());

  it('debe enviar las cookies en solicitudes a la API', () => {
    http.get('/api/autenticacion/sesion').subscribe();

    const solicitud = controladorHttp.expectOne('/api/autenticacion/sesion');
    expect(solicitud.request.withCredentials).toBe(true);
    solicitud.flush({});
  });

  it('debe obtener y adjuntar la protección CSRF en operaciones de escritura', () => {
    http.post('/api/usuarios', { nombreUsuario: 'tecnico.uno' }).subscribe();

    const proteccion = controladorHttp.expectOne('/api/autenticacion/proteccion-csrf');
    expect(proteccion.request.withCredentials).toBe(true);
    proteccion.flush({ token: 'token-de-prueba' });

    const escritura = controladorHttp.expectOne('/api/usuarios');
    expect(escritura.request.withCredentials).toBe(true);
    expect(escritura.request.headers.get('X-CSRF-TOKEN')).toBe('token-de-prueba');
    escritura.flush({});
  });
});
