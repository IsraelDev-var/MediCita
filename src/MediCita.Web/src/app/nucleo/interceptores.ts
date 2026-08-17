import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AutenticacionService } from './autenticacion.service';

/** Adjunta el token JWT a cada llamada a la API. */
export const interceptorDeToken: HttpInterceptorFn = (peticion, siguiente) => {
  const token = inject(AutenticacionService).token;

  if (!token) return siguiente(peticion);

  return siguiente(
    peticion.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
  );
};

/** Si el token venció, cierra la sesión y devuelve al usuario a la pantalla de acceso. */
export const interceptorDeSesionVencida: HttpInterceptorFn = (peticion, siguiente) => {
  const autenticacion = inject(AutenticacionService);

  return siguiente(peticion).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && autenticacion.autenticado()) {
        autenticacion.cerrarSesion();
      }

      return throwError(() => error);
    })
  );
};

/** Extrae el mensaje del ProblemDetails que devuelve la API. */
export function mensajeDeError(error: unknown, respaldo = 'Ocurrió un error inesperado.'): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'No se pudo conectar con el servidor. Verifique que la API esté encendida.';

    const detalle = error.error?.detail ?? error.error?.title;
    if (typeof detalle === 'string' && detalle.trim()) return detalle;
  }

  return respaldo;
}
