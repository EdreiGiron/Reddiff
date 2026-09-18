import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CapturaResumen,
  CapturaRemotaResultado,
  CargaArchivoResultado,
  VersionConfiguracionDetalle,
  VersionConfiguracionResumen,
} from './modelos-capturas';

@Injectable({ providedIn: 'root' })
export class ServicioCapturas {
  private readonly http = inject(HttpClient);

  listarCapturas(dispositivoId?: number): Observable<CapturaResumen[]> {
    return this.http.get<CapturaResumen[]>('/api/capturas', {
      params: this.parametrosDispositivo(dispositivoId),
    });
  }

  listarVersiones(dispositivoId?: number): Observable<VersionConfiguracionResumen[]> {
    return this.http.get<VersionConfiguracionResumen[]>('/api/versiones-configuracion', {
      params: this.parametrosDispositivo(dispositivoId),
    });
  }

  obtenerVersion(versionId: number): Observable<VersionConfiguracionDetalle> {
    return this.http.get<VersionConfiguracionDetalle>(`/api/versiones-configuracion/${versionId}`);
  }

  cargarArchivo(
    dispositivoId: number,
    archivo: File,
    comentario: string,
  ): Observable<CargaArchivoResultado> {
    const formulario = new FormData();
    formulario.append('DispositivoId', dispositivoId.toString());
    formulario.append('Archivo', archivo, archivo.name);
    if (comentario) {
      formulario.append('Comentario', comentario);
    }

    return this.http.post<CargaArchivoResultado>('/api/capturas/archivo', formulario);
  }

  capturarRemotamente(dispositivoId: number): Observable<CapturaRemotaResultado> {
    return this.http.post<CapturaRemotaResultado>('/api/capturas/remota', { dispositivoId });
  }

  private parametrosDispositivo(dispositivoId?: number): HttpParams {
    return dispositivoId
      ? new HttpParams().set('dispositivoId', dispositivoId.toString())
      : new HttpParams();
  }
}
