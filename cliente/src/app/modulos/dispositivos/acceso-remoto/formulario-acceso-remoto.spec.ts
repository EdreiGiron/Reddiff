import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
  AccesoRemotoResumen,
  ConfigurarAccesoRemotoSolicitud,
  DispositivoResumen,
} from '../modelos-dispositivos';
import { ServicioDispositivos } from '../servicio-dispositivos';
import { FormularioAccesoRemoto } from './formulario-acceso-remoto';

describe('FormularioAccesoRemoto', () => {
  const dispositivo: DispositivoResumen = {
    id: 8,
    nombre: 'borde-principal',
    host: '192.0.2.18',
    tipo: 'Router',
    modelo: null,
    protocolo: 'Ssh',
    puerto: 22,
    fuenteEventos: 'Syslog',
    estado: 'Autorizado',
    accesoRemotoConfigurado: true,
  };

  const detalle: AccesoRemotoResumen = {
    dispositivoId: dispositivo.id,
    configurado: true,
    usuarioAcceso: 'usuario-lectura',
    algoritmoClaveHost: 'ssh-ed25519',
    huellaClaveHost: 'a'.repeat(64),
    configuradoEn: '2026-09-05T01:00:00Z',
  };

  it('debe mostrar solo metadatos y solicitar un secreto nuevo', () => {
    configurarPrueba();
    const fixture = TestBed.createComponent(FormularioAccesoRemoto);
    fixture.componentRef.setInput('dispositivo', dispositivo);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    const secreto = elemento.querySelector('#acceso-secreto') as HTMLInputElement;
    expect(elemento.textContent).toContain('usuario-lectura');
    expect(elemento.textContent).toContain('El secreto almacenado no puede consultarse');
    expect(secreto.type).toBe('password');
    expect(secreto.autocomplete).toBe('new-password');
    expect(secreto.value).toBe('');
  });

  it('debe limpiar el secreto después de reemplazar el acceso', () => {
    const solicitudesRecibidas: ConfigurarAccesoRemotoSolicitud[] = [];
    configurarPrueba((solicitud) => solicitudesRecibidas.push(solicitud));
    const fixture = TestBed.createComponent(FormularioAccesoRemoto);
    fixture.componentRef.setInput('dispositivo', dispositivo);
    fixture.detectChanges();

    const instancia = fixture.componentInstance as unknown as {
      formulario: {
        setValue(valor: ConfigurarAccesoRemotoSolicitud): void;
        controls: { secretoAcceso: { value: string } };
      };
      guardar(): void;
    };
    instancia.formulario.setValue({
      usuarioAcceso: 'usuario-lectura',
      secretoAcceso: 'Clave-nueva-solo-prueba',
      algoritmoClaveHost: 'ssh-ed25519',
      huellaClaveHost: 'c'.repeat(64),
      huellaClaveHostConfirmada: true,
    });

    const actualizaciones: AccesoRemotoResumen[] = [];
    fixture.componentInstance.actualizado.subscribe((valor) => actualizaciones.push(valor));
    instancia.guardar();
    fixture.detectChanges();

    expect(solicitudesRecibidas[0].secretoAcceso).toBe('Clave-nueva-solo-prueba');
    expect(instancia.formulario.controls.secretoAcceso.value).toBe('');
    expect(actualizaciones[0].configurado).toBe(true);
    expect(fixture.nativeElement.textContent).not.toContain('Clave-nueva-solo-prueba');
  });

  function configurarPrueba(
    alConfigurar?: (solicitud: ConfigurarAccesoRemotoSolicitud) => void,
  ): void {
    TestBed.configureTestingModule({
      imports: [FormularioAccesoRemoto],
      providers: [
        {
          provide: ServicioDispositivos,
          useValue: {
            obtenerAccesoRemoto: () => of(detalle),
            configurarAccesoRemoto: (
              _dispositivoId: number,
              solicitud: ConfigurarAccesoRemotoSolicitud,
            ) => {
              alConfigurar?.(solicitud);
              return of({ ...detalle, configurado: true });
            },
          },
        },
      ],
    });
  }
});
