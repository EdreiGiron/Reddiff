import { bootstrapApplication } from '@angular/platform-browser';
import { Aplicacion } from './app/aplicacion';
import { configuracionAplicacion } from './app/configuracion-aplicacion';

bootstrapApplication(Aplicacion, configuracionAplicacion).catch((errorInicio: unknown) =>
  console.error(errorInicio),
);
