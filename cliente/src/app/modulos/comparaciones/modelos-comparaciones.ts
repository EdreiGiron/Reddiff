export type TipoDiferencia = 'Agregada' | 'Eliminada' | 'Modificada';

export interface VersionComparadaResumen {
  id: number;
  numero: number;
  hash: string;
  capturadaEn: string;
  estable: boolean;
}

export interface ComparacionResumen {
  id: number;
  dispositivoId: number;
  dispositivo: string;
  versionOrigen: VersionComparadaResumen;
  versionDestino: VersionComparadaResumen;
  usuario: string;
  fecha: string;
  estado: 'Pendiente' | 'EnProceso' | 'Completado' | 'Fallido';
  agregadas: number;
  eliminadas: number;
  modificadas: number;
}

export interface DiferenciaComparacionResumen {
  linea: number;
  tipo: TipoDiferencia;
  textoAnterior: string | null;
  textoNuevo: string | null;
}

export interface ComparacionDetalle extends ComparacionResumen {
  diferencias: DiferenciaComparacionResumen[];
}
