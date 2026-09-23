export type EstadoAuditoria = 'Exitoso' | 'Fallido';

export interface AuditoriaResumen {
  id: number;
  usuarioId: number | null;
  nombreUsuario: string | null;
  accion: string;
  entidad: string;
  entidadId: number | null;
  fecha: string;
  estado: EstadoAuditoria;
  detalle: string | null;
}

export interface ResultadoPaginaAuditorias {
  registros: AuditoriaResumen[];
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}

export interface UsuarioFiltroAuditoria {
  id: number;
  nombreUsuario: string;
}

export interface CatalogoFiltrosAuditoria {
  usuarios: UsuarioFiltroAuditoria[];
  acciones: string[];
  entidades: string[];
}

export interface FiltrosAuditorias {
  usuarioId?: number;
  accion?: string;
  entidad?: string;
  estado?: EstadoAuditoria;
  desde?: string;
  hasta?: string;
  pagina: number;
  tamanoPagina: number;
}
