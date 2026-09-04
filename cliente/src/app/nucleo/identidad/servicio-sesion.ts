import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, of, shareReplay, tap } from 'rxjs';
import { ServicioProteccionCsrf } from '../http/servicio-proteccion-csrf';
import { InicioSesionSolicitud, SesionUsuario } from './modelos-identidad';

@Injectable({ providedIn: 'root' })
export class ServicioSesion {
  private readonly http = inject(HttpClient);
  private readonly proteccionCsrf = inject(ServicioProteccionCsrf);
  private readonly sesionInterna = signal<SesionUsuario | null>(null);
  private readonly inicializadaInterna = signal(false);
  private peticionRestauracion?: Observable<SesionUsuario | null>;

  readonly sesion = this.sesionInterna.asReadonly();
  readonly inicializada = this.inicializadaInterna.asReadonly();
  readonly autenticado = computed(() => this.sesionInterna() !== null);
  readonly esAdministrador = computed(() => this.sesionInterna()?.rol === 'Administrador');

  asegurarSesion(): Observable<SesionUsuario | null> {
    if (this.inicializadaInterna()) {
      return of(this.sesionInterna());
    }

    if (!this.peticionRestauracion) {
      this.peticionRestauracion = this.http.get<SesionUsuario>('/api/autenticacion/sesion').pipe(
        tap((sesion) => this.sesionInterna.set(sesion)),
        catchError(() => {
          this.sesionInterna.set(null);
          return of(null);
        }),
        finalize(() => {
          this.inicializadaInterna.set(true);
          this.peticionRestauracion = undefined;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }

    return this.peticionRestauracion;
  }

  iniciarSesion(solicitud: InicioSesionSolicitud): Observable<SesionUsuario> {
    return this.http.post<SesionUsuario>('/api/autenticacion/iniciar-sesion', solicitud).pipe(
      tap((sesion) => {
        this.sesionInterna.set(sesion);
        this.inicializadaInterna.set(true);
        this.proteccionCsrf.invalidar();
      }),
    );
  }

  cerrarSesion(): Observable<void> {
    return this.http.post<void>('/api/autenticacion/cerrar-sesion', null).pipe(
      finalize(() => {
        this.descartarSesionLocal();
      }),
    );
  }

  descartarSesionLocal(): void {
    this.sesionInterna.set(null);
    this.inicializadaInterna.set(true);
    this.proteccionCsrf.invalidar();
  }
}
