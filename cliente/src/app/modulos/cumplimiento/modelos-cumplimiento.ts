export type AlcanceBaseline = 'Dispositivo' | 'TipoDispositivo';

export type CriterioReglaBaseline = 'Contiene' | 'NoContiene' | 'CoincideExpresionRegular';

export type EstadoResultadoRegla = 'Cumplida' | 'Incumplida' | 'NoEvaluable';

export type ResultadoGeneralVerificacion = 'Cumple' | 'Incumple' | 'NoEvaluable';

export interface CrearReglaBaselineSolicitud {
  criterio: CriterioReglaBaseline;
  esperado: string;
  obligatoria: boolean;
}

export interface CrearBaselineSolicitud {
  nombre: string;
  alcance: AlcanceBaseline;
  dispositivoId: number | null;
  tipoDispositivo: string | null;
  versionReferenciaId: number | null;
  reglas: CrearReglaBaselineSolicitud[];
}

export interface ReglaBaselineResumen {
  id: number;
  criterio: CriterioReglaBaseline;
  esperado: string;
  obligatoria: boolean;
}

export interface VersionReferenciaBaselineResumen {
  id: number;
  numero: number;
  dispositivo: string;
  hash: string;
  capturadaEn: string;
  estable: boolean;
}

export interface BaselineResumen {
  id: number;
  nombre: string;
  alcance: AlcanceBaseline;
  dispositivoId: number | null;
  dispositivo: string | null;
  tipoDispositivo: string | null;
  activa: boolean;
  versionReferencia: VersionReferenciaBaselineResumen | null;
  reglas: ReglaBaselineResumen[];
}

export interface ResultadoReglaResumen {
  reglaId: number;
  criterio: CriterioReglaBaseline;
  esperado: string;
  obligatoria: boolean;
  estado: EstadoResultadoRegla;
  evidencia: string;
}

export interface VerificacionResumen {
  id: number;
  baselineId: number;
  baseline: string;
  dispositivoId: number;
  dispositivo: string;
  versionId: number;
  version: number;
  usuario: string;
  fecha: string;
  estado: 'Pendiente' | 'EnProceso' | 'Completado' | 'Fallido';
  resultadoGeneral: ResultadoGeneralVerificacion;
  totalReglas: number;
  cumplidas: number;
  incumplidas: number;
  noEvaluables: number;
  porcentajeCumplimiento: number;
}

export interface VerificacionDetalle extends VerificacionResumen {
  resultados: ResultadoReglaResumen[];
}
