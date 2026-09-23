import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CatalogoFiltrosAuditoria,
  FiltrosAuditorias,
  ResultadoPaginaAuditorias,
} from './modelos-auditorias';

@Injectable({ providedIn: 'root' })
export class ServicioAuditorias {
  private readonly http = inject(HttpClient);

  obtenerCatalogoFiltros(): Observable<CatalogoFiltrosAuditoria> {
    return this.http.get<CatalogoFiltrosAuditoria>('/api/auditorias/catalogo-filtros');
  }

  listar(filtros: FiltrosAuditorias): Observable<ResultadoPaginaAuditorias> {
    let parametros = new HttpParams()
      .set('pagina', filtros.pagina)
      .set('tamanoPagina', filtros.tamanoPagina);

    for (const [nombre, valor] of Object.entries(filtros)) {
      if (nombre === 'pagina' || nombre === 'tamanoPagina' || valor === undefined || valor === '') {
        continue;
      }

      parametros = parametros.set(nombre, String(valor));
    }

    return this.http.get<ResultadoPaginaAuditorias>('/api/auditorias', { params: parametros });
  }
}
