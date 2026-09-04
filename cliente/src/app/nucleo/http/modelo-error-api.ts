import { HttpErrorResponse } from '@angular/common/http';

interface CuerpoErrorApi {
  detail?: unknown;
  mensaje?: unknown;
  title?: unknown;
}

export function obtenerMensajeError(error: unknown, mensajePredeterminado: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return mensajePredeterminado;
  }

  const cuerpo = error.error as CuerpoErrorApi | string | null;
  if (typeof cuerpo === 'string' && cuerpo.trim()) {
    return cuerpo;
  }

  if (cuerpo && typeof cuerpo === 'object') {
    for (const valor of [cuerpo.detail, cuerpo.mensaje, cuerpo.title]) {
      if (typeof valor === 'string' && valor.trim()) {
        return valor;
      }
    }
  }

  return mensajePredeterminado;
}
