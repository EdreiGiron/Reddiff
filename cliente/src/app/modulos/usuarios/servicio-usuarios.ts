import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ActualizarUsuarioSolicitud,
  CrearUsuarioSolicitud,
  RolResumen,
  UsuarioResumen,
} from './modelos-usuarios';

@Injectable({ providedIn: 'root' })
export class ServicioUsuarios {
  private readonly http = inject(HttpClient);

  listarUsuarios(): Observable<UsuarioResumen[]> {
    return this.http.get<UsuarioResumen[]>('/api/usuarios');
  }

  listarRoles(): Observable<RolResumen[]> {
    return this.http.get<RolResumen[]>('/api/roles');
  }

  crearUsuario(solicitud: CrearUsuarioSolicitud): Observable<UsuarioResumen> {
    return this.http.post<UsuarioResumen>('/api/usuarios', solicitud);
  }

  actualizarUsuario(
    usuarioId: number,
    solicitud: ActualizarUsuarioSolicitud,
  ): Observable<UsuarioResumen> {
    return this.http.put<UsuarioResumen>(`/api/usuarios/${usuarioId}`, solicitud);
  }

  cambiarEstado(usuarioId: number, activo: boolean): Observable<UsuarioResumen> {
    return this.http.patch<UsuarioResumen>(`/api/usuarios/${usuarioId}/estado`, { activo });
  }
}
