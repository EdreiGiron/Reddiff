import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { DispositivoResumen } from './modelos-dispositivos';
import { PaginaDispositivos } from './pagina-dispositivos';
import { ServicioDispositivos } from './servicio-dispositivos';

describe('PaginaDispositivos', () => {
  const dispositivos: DispositivoResumen[] = [
    {
      id: 1,
      nombre: 'nucleo-principal',
      host: 'core-01.ejemplo.local',
      tipo: 'Router',
      modelo: 'ISR 4331',
      protocolo: 'Ssh',
      puerto: 22,
      fuenteEventos: 'Syslog',
      estado: 'Autorizado',
    },
  ];

  it('debe mostrar inventario y acciones al administrador', () => {
    configurarPrueba('Administrador');
    const fixture = TestBed.createComponent(PaginaDispositivos);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.textContent).toContain('nucleo-principal');
    expect(elemento.textContent).toContain('core-01.ejemplo.local:22');
    expect(elemento.textContent).toContain('Nuevo dispositivo');
    expect(elemento.textContent).toContain('Revocar');
  });

  it('debe ofrecer consulta sin acciones administrativas al técnico', () => {
    configurarPrueba('Tecnico');
    const fixture = TestBed.createComponent(PaginaDispositivos);
    fixture.detectChanges();

    const texto = fixture.nativeElement.textContent as string;
    expect(texto).toContain('nucleo-principal');
    expect(texto).toContain('Tu rol permite consultar el inventario');
    expect(texto).not.toContain('Nuevo dispositivo');
    expect(texto).not.toContain('Revocar');
  });

  it('debe cambiar el puerto sugerido al seleccionar NETCONF', () => {
    configurarPrueba('Administrador');
    const fixture = TestBed.createComponent(PaginaDispositivos);
    fixture.detectChanges();

    const instancia = fixture.componentInstance as unknown as {
      formulario: {
        controls: { protocolo: { setValue(valor: string): void }; puerto: { value: number } };
      };
      alCambiarProtocolo(): void;
    };
    instancia.formulario.controls.protocolo.setValue('Netconf');
    instancia.alCambiarProtocolo();

    expect(instancia.formulario.controls.puerto.value).toBe(830);
  });

  function configurarPrueba(rol: 'Administrador' | 'Tecnico'): void {
    TestBed.configureTestingModule({
      imports: [PaginaDispositivos],
      providers: [
        {
          provide: ServicioDispositivos,
          useValue: { listar: () => of(dispositivos) },
        },
        {
          provide: ServicioSesion,
          useValue: {
            sesion: signal({
              usuarioId: 1,
              nombreUsuario: 'usuario.pruebas',
              rol,
              expiraEn: null,
            }),
            esAdministrador: signal(rol === 'Administrador'),
          },
        },
      ],
    });
  }
});
