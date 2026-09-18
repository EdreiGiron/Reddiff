import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { PaginaInicio } from './pagina-inicio';

describe('PaginaInicio', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PaginaInicio],
      providers: [
        provideRouter([]),
        {
          provide: ServicioSesion,
          useValue: {
            sesion: signal({
              usuarioId: 1,
              nombreUsuario: 'administrador.principal',
              rol: 'Administrador',
              expiraEn: null,
            }),
            esAdministrador: signal(true),
          },
        },
      ],
    });
  });

  it('debe mostrar el usuario y las funciones permitidas', () => {
    const fixture = TestBed.createComponent(PaginaInicio);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.querySelector('h1')?.textContent).toContain('administrador.principal');
    expect(elemento.textContent).toContain('Usuarios y roles');
    expect(elemento.textContent).toContain('Dispositivos');
    expect(elemento.textContent).toContain('Comparación diferencial');
  });
});
