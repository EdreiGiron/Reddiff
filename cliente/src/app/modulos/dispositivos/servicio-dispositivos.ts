import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AccesoRemotoResumen,
  ConfigurarAccesoRemotoSolicitud,
  DispositivoResumen,
  EstadoDispositivo,
  GuardarDispositivoSolicitud,
} from './modelos-dispositivos';

@Injectable({ providedIn: 'root' })
export class ServicioDispositivos {
  private readonly http = inject(HttpClient);

  listar(): Observable<DispositivoResumen[]> {
    return this.http.get<DispositivoResumen[]>('/api/dispositivos');
  }

  crear(solicitud: GuardarDispositivoSolicitud): Observable<DispositivoResumen> {
    return this.http.post<DispositivoResumen>('/api/dispositivos', solicitud);
  }

  actualizar(
    dispositivoId: number,
    solicitud: GuardarDispositivoSolicitud,
  ): Observable<DispositivoResumen> {
    return this.http.put<DispositivoResumen>(`/api/dispositivos/${dispositivoId}`, solicitud);
  }

  cambiarEstado(dispositivoId: number, estado: EstadoDispositivo): Observable<DispositivoResumen> {
    return this.http.patch<DispositivoResumen>(`/api/dispositivos/${dispositivoId}/estado`, {
      estado,
    });
  }

  obtenerAccesoRemoto(dispositivoId: number): Observable<AccesoRemotoResumen> {
    return this.http.get<AccesoRemotoResumen>(`/api/dispositivos/${dispositivoId}/acceso-remoto`);
  }

  configurarAccesoRemoto(
    dispositivoId: number,
    solicitud: ConfigurarAccesoRemotoSolicitud,
  ): Observable<AccesoRemotoResumen> {
    return this.http.put<AccesoRemotoResumen>(
      `/api/dispositivos/${dispositivoId}/acceso-remoto`,
      solicitud,
    );
  }

  revocarAccesoRemoto(dispositivoId: number): Observable<AccesoRemotoResumen> {
    return this.http.delete<AccesoRemotoResumen>(
      `/api/dispositivos/${dispositivoId}/acceso-remoto`,
    );
  }
}
