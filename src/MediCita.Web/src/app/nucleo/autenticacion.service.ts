import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { RespuestaAutenticacion, Rol, Usuario } from './modelos';

const CLAVE_TOKEN = 'medicita.token';
const CLAVE_USUARIO = 'medicita.usuario';

/**
 * Guarda la sesión y expone el usuario como señal, para que la barra superior y
 * los guards reaccionen sin suscripciones manuales.
 */
@Injectable({ providedIn: 'root' })
export class AutenticacionService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _usuario = signal<Usuario | null>(this.leerUsuarioGuardado());

  readonly usuario = this._usuario.asReadonly();
  readonly autenticado = computed(() => this._usuario() !== null);
  readonly rol = computed<Rol | null>(() => this._usuario()?.rolNombre ?? null);

  get token(): string | null {
    return localStorage.getItem(CLAVE_TOKEN);
  }

  iniciarSesion(correo: string, contrasena: string): Observable<RespuestaAutenticacion> {
    return this.http
      .post<RespuestaAutenticacion>(`${environment.urlApi}/auth/login`, { correo, contrasena })
      .pipe(tap((r) => this.guardar(r)));
  }

  registrar(datos: {
    cedula: string;
    nombre: string;
    apellido: string;
    correo: string;
    telefono: string | null;
    contrasena: string;
    fechaNacimiento: string | null;
  }): Observable<RespuestaAutenticacion> {
    return this.http
      .post<RespuestaAutenticacion>(`${environment.urlApi}/auth/registro`, datos)
      .pipe(tap((r) => this.guardar(r)));
  }

  cerrarSesion(): void {
    localStorage.removeItem(CLAVE_TOKEN);
    localStorage.removeItem(CLAVE_USUARIO);
    this._usuario.set(null);
    this.router.navigate(['/acceso']);
  }

  /** Cada rol entra por su propia pantalla, como indican los mockups. */
  rutaInicial(rol: Rol): string {
    switch (rol) {
      case 'Medico':
        return '/medico/agenda';
      case 'Administrador':
        return '/admin';
      default:
        return '/citas';
    }
  }

  private guardar(respuesta: RespuestaAutenticacion): void {
    localStorage.setItem(CLAVE_TOKEN, respuesta.token);
    localStorage.setItem(CLAVE_USUARIO, JSON.stringify(respuesta.usuario));
    this._usuario.set(respuesta.usuario);
  }

  private leerUsuarioGuardado(): Usuario | null {
    const crudo = localStorage.getItem(CLAVE_USUARIO);
    if (!crudo) return null;

    try {
      return JSON.parse(crudo) as Usuario;
    } catch {
      localStorage.removeItem(CLAVE_USUARIO);
      return null;
    }
  }
}
