import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BaselineResumen,
  CrearBaselineSolicitud,
  VerificacionDetalle,
  VerificacionResumen,
} from './modelos-cumplimiento';

@Injectable({ providedIn: 'root' })
export class ServicioCumplimiento {
  private readonly http = inject(HttpClient);

  listarBaselines(): Observable<BaselineResumen[]> {
    return this.http.get<BaselineResumen[]>('/api/baselines');
  }

  crearBaseline(solicitud: CrearBaselineSolicitud): Observable<BaselineResumen> {
    return this.http.post<BaselineResumen>('/api/baselines', solicitud);
  }

  cambiarEstadoBaseline(baselineId: number, activa: boolean): Observable<BaselineResumen> {
    return this.http.patch<BaselineResumen>(`/api/baselines/${baselineId}/estado`, { activa });
  }

  listarVerificaciones(): Observable<VerificacionResumen[]> {
    return this.http.get<VerificacionResumen[]>('/api/verificaciones');
  }

  obtenerVerificacion(verificacionId: number): Observable<VerificacionDetalle> {
    return this.http.get<VerificacionDetalle>(`/api/verificaciones/${verificacionId}`);
  }

  verificar(baselineId: number, versionId: number): Observable<VerificacionDetalle> {
    return this.http.post<VerificacionDetalle>('/api/verificaciones', {
      baselineId,
      versionId,
    });
  }
}
