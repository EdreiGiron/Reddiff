import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, finalize, map, of, shareReplay } from 'rxjs';

interface RespuestaProteccionCsrf {
  token: string;
}

@Injectable({ providedIn: 'root' })
export class ServicioProteccionCsrf {
  private readonly http = inject(HttpClient);
  private token?: string;
  private peticionToken?: Observable<string>;

  obtenerToken(): Observable<string> {
    if (this.token) {
      return of(this.token);
    }

    if (!this.peticionToken) {
      this.peticionToken = this.http
        .get<RespuestaProteccionCsrf>('/api/autenticacion/proteccion-csrf')
        .pipe(
          map((respuesta) => {
            this.token = respuesta.token;
            return respuesta.token;
          }),
          finalize(() => {
            this.peticionToken = undefined;
          }),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }

    return this.peticionToken;
  }

  invalidar(): void {
    this.token = undefined;
  }
}
