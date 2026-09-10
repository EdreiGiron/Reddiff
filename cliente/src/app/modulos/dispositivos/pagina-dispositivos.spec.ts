import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { AccesoRemotoResumen, DispositivoResumen } from './modelos-dispositivos';
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
      accesoRemotoConfigurado: false,
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
    expect(elemento.textContent).toContain('Acciones');
    expect(elemento.textContent).toContain('Desautorizar');
    expect(elemento.querySelectorAll('.grupo-acciones button').length).toBe(4);
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

  it('debe solicitar un secreto nuevo sin mostrar el valor almacenado', () => {
    const configurado: DispositivoResumen = {
      ...dispositivos[0],
      accesoRemotoConfigurado: true,
    };
    const detalle: AccesoRemotoResumen = {
      dispositivoId: configurado.id,
      configurado: true,
      usuarioAcceso: 'usuario-lectura',
      algoritmoClaveHost: 'ssh-ed25519',
      huellaClaveHost: 'a'.repeat(64),
      configuradoEn: '2026-09-05T01:00:00Z',
    };
    configurarPrueba('Administrador', [configurado], detalle);
    const fixture = TestBed.createComponent(PaginaDispositivos);
    fixture.detectChanges();

    const botones = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    botones.find((boton) => boton.textContent?.includes('Reemplazar acceso'))?.click();
    fixture.detectChanges();

    const secreto = fixture.nativeElement.querySelector('#acceso-secreto') as HTMLInputElement;
    const texto = fixture.nativeElement.textContent as string;
    expect(secreto.type).toBe('password');
    expect(secreto.value).toBe('');
    expect(texto).toContain('El secreto almacenado no puede consultarse');
    expect(texto).not.toContain('secreto-protegido-interno');
  });

  function configurarPrueba(
    rol: 'Administrador' | 'Tecnico',
    lista: DispositivoResumen[] = dispositivos,
    detalle: AccesoRemotoResumen = {
      dispositivoId: 1,
      configurado: false,
      usuarioAcceso: null,
      algoritmoClaveHost: null,
      huellaClaveHost: null,
      configuradoEn: null,
    },
  ): void {
    const servicioDispositivos = {
      listar: () => of(lista),
      obtenerAccesoRemoto: () => of(detalle),
      configurarAccesoRemoto: () => of({ ...detalle, configurado: true }),
      revocarAccesoRemoto: () => of({ ...detalle, configurado: false }),
    };

    TestBed.configureTestingModule({
      imports: [PaginaDispositivos],
      providers: [
        {
          provide: ServicioDispositivos,
          useValue: servicioDispositivos,
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
