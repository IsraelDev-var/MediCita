import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { AutenticacionService } from '../../nucleo/autenticacion.service';
import { mensajeDeError } from '../../nucleo/interceptores';

/** Pantalla "Mi perfil": datos de contacto y cambio de contraseña, para los tres roles. */
@Component({
  selector: 'mc-perfil',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="contenedor angosto">
      <h1>Mi perfil</h1>

      @if (usuario(); as u) {
        <section class="panel">
          <div class="etiqueta">Datos de la cuenta</div>
          <dl>
            <div><dt>Nombre</dt><dd>{{ u.nombreCompleto }}</dd></div>
            <div><dt>Cédula</dt><dd>{{ u.cedula }}</dd></div>
            <div><dt>Rol</dt><dd>{{ u.rolNombre }}</dd></div>
          </dl>
        </section>

        @if (aviso(); as mensaje) {
          <div class="aviso aviso-info">{{ mensaje }}</div>
        }
        @if (error(); as mensaje) {
          <div class="aviso aviso-error" role="alert">{{ mensaje }}</div>
        }

        <section class="panel">
          <div class="etiqueta">Contacto</div>
          <form [formGroup]="formularioContacto" (ngSubmit)="guardarContacto()" novalidate>
            <div class="campo">
              <label for="correo">Correo electrónico</label>
              <input id="correo" type="email" formControlName="correo" />
            </div>
            <div class="campo">
              <label for="telefono">Teléfono</label>
              <input id="telefono" type="tel" formControlName="telefono" />
            </div>
            <button type="submit" class="boton" [disabled]="guardando()">Guardar contacto</button>
          </form>
        </section>

        <section class="panel">
          <div class="etiqueta">Contraseña</div>
          <form [formGroup]="formularioContrasena" (ngSubmit)="cambiarContrasena()" novalidate>
            <div class="campo">
              <label for="actual">Contraseña actual</label>
              <input id="actual" type="password" formControlName="actual" autocomplete="current-password" />
            </div>
            <div class="campo">
              <label for="nueva">Contraseña nueva</label>
              <input id="nueva" type="password" formControlName="nueva" autocomplete="new-password" />
              <small class="suave">Mínimo 8 caracteres.</small>
            </div>
            <button type="submit" class="boton" [disabled]="guardando()">Cambiar contraseña</button>
          </form>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .angosto {
        max-width: 620px;
      }

      h1 {
        margin-bottom: 20px;
      }

      section {
        margin-bottom: 18px;
      }

      dl {
        margin: 14px 0 0;
      }

      dl > div {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        padding: 9px 0;
        border-bottom: 1px solid var(--borde);
      }

      dl > div:last-child {
        border-bottom: none;
      }

      dt {
        color: var(--texto-suave);
        font-size: 14px;
      }

      dd {
        margin: 0;
        font-weight: 600;
        font-size: 14px;
      }

      form {
        margin-top: 16px;
      }

      small {
        display: block;
        margin-top: 6px;
        font-size: 12px;
      }

      .aviso {
        margin-bottom: 18px;
      }
    `,
  ],
})
export class PerfilComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly autenticacion = inject(AutenticacionService);

  readonly usuario = this.autenticacion.usuario;
  readonly guardando = signal(false);
  readonly aviso = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly formularioContacto = this.fb.nonNullable.group({
    correo: [this.usuario()?.correo ?? '', [Validators.required, Validators.email]],
    telefono: [this.usuario()?.telefono ?? ''],
  });

  readonly formularioContrasena = this.fb.nonNullable.group({
    actual: ['', Validators.required],
    nueva: ['', [Validators.required, Validators.minLength(8)]],
  });

  guardarContacto(): void {
    if (this.formularioContacto.invalid) {
      this.formularioContacto.markAllAsTouched();
      return;
    }

    const { correo, telefono } = this.formularioContacto.getRawValue();
    this.iniciar();

    this.http.put(`${environment.urlApi}/auth/contacto`, { correo, telefono: telefono || null }).subscribe({
      next: () => this.terminar('Datos de contacto actualizados. Vuelve a iniciar sesión para verlos en la barra.'),
      error: (e) => this.fallar(e, 'No se pudo actualizar el contacto.'),
    });
  }

  cambiarContrasena(): void {
    if (this.formularioContrasena.invalid) {
      this.formularioContrasena.markAllAsTouched();
      return;
    }

    const { actual, nueva } = this.formularioContrasena.getRawValue();
    this.iniciar();

    this.http.put(`${environment.urlApi}/auth/contrasena`, { actual, nueva }).subscribe({
      next: () => {
        this.terminar('Contraseña actualizada.');
        this.formularioContrasena.reset();
      },
      error: (e) => this.fallar(e, 'No se pudo cambiar la contraseña.'),
    });
  }

  private iniciar(): void {
    this.guardando.set(true);
    this.aviso.set(null);
    this.error.set(null);
  }

  private terminar(mensaje: string): void {
    this.guardando.set(false);
    this.aviso.set(mensaje);
  }

  private fallar(error: unknown, respaldo: string): void {
    this.guardando.set(false);
    this.error.set(mensajeDeError(error, respaldo));
  }
}
