import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ComparacionDetalle, ComparacionResumen } from './modelos-comparaciones';

@Injectable({ providedIn: 'root' })
export class ServicioComparaciones {
  private readonly http = inject(HttpClient);

  listar(dispositivoId?: number): Observable<ComparacionResumen[]> {
    const params = dispositivoId
      ? new HttpParams().set('dispositivoId', dispositivoId.toString())
      : new HttpParams();
    return this.http.get<ComparacionResumen[]>('/api/comparaciones', { params });
  }

  obtener(comparacionId: number): Observable<ComparacionDetalle> {
    return this.http.get<ComparacionDetalle>(`/api/comparaciones/${comparacionId}`);
  }

  comparar(versionOrigenId: number, versionDestinoId: number): Observable<ComparacionDetalle> {
    return this.http.post<ComparacionDetalle>('/api/comparaciones', {
      versionOrigenId,
      versionDestinoId,
    });
  }
}
