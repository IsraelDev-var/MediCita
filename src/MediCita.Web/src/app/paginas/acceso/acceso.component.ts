import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AutenticacionService } from '../../nucleo/autenticacion.service';
import { mensajeDeError } from '../../nucleo/interceptores';

/**
 * Pantalla 01. Una sola vista con dos pestañas: iniciar sesión y crear cuenta.
 * El token que devuelve la API decide a qué pantalla se redirige.
 */
@Component({
  selector: 'mc-acceso',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './acceso.component.html',
  styleUrl: './acceso.component.scss',
})
export class AccesoComponent {
  private readonly fb = inject(FormBuilder);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly router = inject(Router);
  private readonly ruta = inject(ActivatedRoute);

  readonly pestana = signal<'entrar' | 'registro'>('entrar');
  readonly enviando = signal(false);
  readonly error = signal<string | null>(null);
  readonly verContrasena = signal(false);

  readonly formularioAcceso = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
    contrasena: ['', [Validators.required]],
    mantenerSesion: [true],
  });

  readonly formularioRegistro = this.fb.nonNullable.group({
    cedula: ['', [Validators.required, Validators.minLength(11)]],
    nombre: ['', Validators.required],
    apellido: ['', Validators.required],
    correo: ['', [Validators.required, Validators.email]],
    telefono: [''],
    fechaNacimiento: [''],
    contrasena: ['', [Validators.required, Validators.minLength(8)]],
  });

  cambiarPestana(pestana: 'entrar' | 'registro'): void {
    this.pestana.set(pestana);
    this.error.set(null);
  }

  entrar(): void {
    if (this.formularioAcceso.invalid) {
      this.formularioAcceso.markAllAsTouched();
      return;
    }

    const { correo, contrasena } = this.formularioAcceso.getRawValue();

    this.enviando.set(true);
    this.error.set(null);

    this.autenticacion.iniciarSesion(correo, contrasena).subscribe({
      next: (respuesta) => this.entrarA(respuesta.usuario.rolNombre),
      error: (e) => {
        this.error.set(mensajeDeError(e, 'No se pudo iniciar sesión.'));
        this.enviando.set(false);
      },
    });
  }

  registrar(): void {
    if (this.formularioRegistro.invalid) {
      this.formularioRegistro.markAllAsTouched();
      return;
    }

    const datos = this.formularioRegistro.getRawValue();

    this.enviando.set(true);
    this.error.set(null);

    this.autenticacion
      .registrar({
        cedula: datos.cedula,
        nombre: datos.nombre,
        apellido: datos.apellido,
        correo: datos.correo,
        telefono: datos.telefono || null,
        contrasena: datos.contrasena,
        fechaNacimiento: datos.fechaNacimiento || null,
      })
      .subscribe({
        next: (respuesta) => this.entrarA(respuesta.usuario.rolNombre),
        error: (e) => {
          this.error.set(mensajeDeError(e, 'No se pudo crear la cuenta.'));
          this.enviando.set(false);
        },
      });
  }

  /** Atajo para la demostración: rellena las credenciales de un usuario de prueba. */
  usarDemostracion(correo: string): void {
    this.pestana.set('entrar');
    this.formularioAcceso.patchValue({ correo, contrasena: 'MediCita2026' });
  }

  private entrarA(rol: 'Paciente' | 'Medico' | 'Administrador'): void {
    const volverA = this.ruta.snapshot.queryParamMap.get('volverA');
    this.router.navigateByUrl(volverA ?? this.autenticacion.rutaInicial(rol));
  }
}
