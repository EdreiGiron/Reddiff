export interface UsuarioResumen {
  id: number;
  nombreUsuario: string;
  rolId: number;
  rol: string;
  activo: boolean;
}

export interface RolResumen {
  id: number;
  nombre: string;
  activo: boolean;
}

export interface CrearUsuarioSolicitud {
  nombreUsuario: string;
  rolId: number;
  contrasena: string;
}

export interface ActualizarUsuarioSolicitud {
  nombreUsuario: string;
  rolId: number;
  nuevaContrasena: string | null;
}
