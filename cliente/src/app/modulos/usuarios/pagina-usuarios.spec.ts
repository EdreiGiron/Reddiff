import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { UsuarioResumen } from './modelos-usuarios';
import { PaginaUsuarios } from './pagina-usuarios';
import { ServicioUsuarios } from './servicio-usuarios';

describe('PaginaUsuarios', () => {
  const usuarios: UsuarioResumen[] = [
    {
      id: 1,
      nombreUsuario: 'administrador.principal',
      rolId: 1,
      rol: 'Administrador',
      activo: true,
    },
    {
      id: 2,
      nombreUsuario: 'tecnico.uno',
      rolId: 2,
      rol: 'Tecnico',
      activo: true,
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PaginaUsuarios],
      providers: [
        provideRouter([]),
        {
          provide: ServicioUsuarios,
          useValue: {
            listarUsuarios: () => of(usuarios),
            listarRoles: () =>
              of([
                { id: 1, nombre: 'Administrador', activo: true },
                { id: 2, nombre: 'Tecnico', activo: true },
              ]),
          },
        },
        {
          provide: ServicioSesion,
          useValue: {
            sesion: signal({
              usuarioId: 1,
              nombreUsuario: 'administrador.principal',
              rol: 'Administrador',
              expiraEn: null,
            }),
          },
        },
      ],
    });
  });

  it('debe mostrar los usuarios y sus estados', () => {
    const fixture = TestBed.createComponent(PaginaUsuarios);
    fixture.detectChanges();

    const texto = fixture.nativeElement.textContent as string;
    expect(texto).toContain('administrador.principal');
    expect(texto).toContain('tecnico.uno');
    expect(texto).toContain('Activos');
  });

  it('debe impedir que el administrador desactive su propia sesión', () => {
    const fixture = TestBed.createComponent(PaginaUsuarios);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    const botones = Array.from(elemento.querySelectorAll<HTMLButtonElement>('tbody button'));
    const botonDesactivarPropio = botones.find(
      (boton) => boton.textContent?.trim() === 'Desactivar' && boton.disabled,
    );

    expect(botonDesactivarPropio).toBeTruthy();
  });
});
