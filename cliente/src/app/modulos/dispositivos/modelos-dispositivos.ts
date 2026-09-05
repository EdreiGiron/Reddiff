export type ProtocoloDispositivo = 'Ssh' | 'Netconf';

export type FuenteEventosDispositivo = 'SnmpTrap' | 'SnmpInform' | 'Syslog';

export type EstadoDispositivo = 'NoAutorizado' | 'Autorizado' | 'Inactivo';

export interface DispositivoResumen {
  id: number;
  nombre: string;
  host: string;
  tipo: string;
  modelo: string | null;
  protocolo: ProtocoloDispositivo;
  puerto: number;
  fuenteEventos: FuenteEventosDispositivo | null;
  estado: EstadoDispositivo;
}

export interface GuardarDispositivoSolicitud {
  nombre: string;
  host: string;
  tipo: string;
  modelo: string | null;
  protocolo: ProtocoloDispositivo;
  puerto: number;
  fuenteEventos: FuenteEventosDispositivo | null;
}
