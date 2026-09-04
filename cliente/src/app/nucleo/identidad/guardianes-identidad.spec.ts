import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { guardianAdministrador } from './guardian-administrador';
import { guardianAutenticacion } from './guardian-autenticacion';
import { SesionUsuario } from './modelos-identidad';
import { ServicioSesion } from './servicio-sesion';

describe('guardianes de identidad', () => {
  const administrador: SesionUsuario = {
    usuarioId: 1,
    nombreUsuario: 'administrador.principal',
    rol: 'Administrador',
    expiraEn: null,
  };

  it('debe dirigir al inicio de sesión cuando no existe una sesión activa', async () => {
    const resultado = await ejecutarGuardian(guardianAutenticacion, null, '/usuarios');

    expect(resultado).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(resultado as UrlTree)).toBe(
      '/iniciar-sesion?retorno=%2Fusuarios',
    );
  });

  it('debe permitir a un administrador abrir la gestión de usuarios', async () => {
    const resultado = await ejecutarGuardian(guardianAdministrador, administrador, '/usuarios');

    expect(resultado).toBe(true);
  });

  it('debe devolver al inicio a un usuario técnico que solicite administración', async () => {
    const resultado = await ejecutarGuardian(
      guardianAdministrador,
      { ...administrador, rol: 'Tecnico' },
      '/usuarios',
    );

    expect(resultado).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(resultado as UrlTree)).toBe('/');
  });
});

async function ejecutarGuardian(
  guardian: typeof guardianAutenticacion,
  sesion: SesionUsuario | null,
  url: string,
): Promise<boolean | UrlTree> {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      {
        provide: ServicioSesion,
        useValue: { asegurarSesion: () => of(sesion) },
      },
    ],
  });

  const resultado = TestBed.runInInjectionContext(() => guardian({} as never, { url } as never));

  if (isObservable(resultado)) {
    return (await firstValueFrom(resultado)) as boolean | UrlTree;
  }

  return (await Promise.resolve(resultado)) as boolean | UrlTree;
}
