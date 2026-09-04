export type RolSesion = 'Administrador' | 'Tecnico';

export interface SesionUsuario {
  usuarioId: number;
  nombreUsuario: string;
  rol: RolSesion;
  expiraEn: string | null;
}

export interface InicioSesionSolicitud {
  nombreUsuario: string;
  contrasena: string;
}
