import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from './autenticacion.service';
import { Rol } from './modelos';

/** Exige sesión iniciada. */
export const guardSesion: CanActivateFn = (_ruta, estado) => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  if (autenticacion.autenticado()) return true;

  return router.createUrlTree(['/acceso'], { queryParams: { volverA: estado.url } });
};

/** Exige además un rol concreto; si no lo tiene, lo manda a su propia pantalla. */
export function guardRol(...roles: Rol[]): CanActivateFn {
  return (_ruta, estado) => {
    const autenticacion = inject(AutenticacionService);
    const router = inject(Router);
    const rol = autenticacion.rol();

    if (!rol) {
      return router.createUrlTree(['/acceso'], { queryParams: { volverA: estado.url } });
    }

    return roles.includes(rol) ? true : router.createUrlTree([autenticacion.rutaInicial(rol)]);
  };
}
