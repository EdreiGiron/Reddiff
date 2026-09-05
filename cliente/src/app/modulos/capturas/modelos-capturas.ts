export type EstadoCaptura = 'Pendiente' | 'EnProceso' | 'Completada' | 'Fallida' | 'Cancelada';

export type OrigenVersion = 'CapturaSsh' | 'CapturaNetconf' | 'Archivo';

export interface CapturaResumen {
  id: number;
  dispositivoId: number;
  dispositivo: string;
  usuarioSolicitante: string | null;
  disparador: string;
  medio: string;
  estado: EstadoCaptura;
  fecha: string;
  error: string | null;
  versionId: number | null;
}

export interface VersionConfiguracionResumen {
  id: number;
  dispositivoId: number;
  dispositivo: string;
  capturaId: number;
  numero: number;
  origen: OrigenVersion;
  hash: string;
  capturadaEn: string;
  comentario: string | null;
  estado: 'Activa' | 'Retirada';
  estable: boolean;
  usuarioSolicitante: string | null;
}

export interface VersionConfiguracionDetalle extends VersionConfiguracionResumen {
  contenido: string;
}

export interface CargaArchivoResultado {
  captura: CapturaResumen;
  version: VersionConfiguracionResumen;
}
