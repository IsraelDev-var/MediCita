import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { fechaLarga, hoyIso, sumarDias } from '../../nucleo/fechas';
import { mensajeDeError } from '../../nucleo/interceptores';
import { MediCitaService } from '../../nucleo/medicita.service';
import { Especialidad, EstadoMedico, Medico, Paciente, ResumenOperativo, Sucursal } from '../../nucleo/modelos';

type Seccion = 'resumen' | 'medicos' | 'pacientes';

/**
 * Pantalla 06. El ausentismo va primero porque es la métrica que justifica el
 * recordatorio automático, y el bloque de estado muestra API, worker y cola SMTP
 * como procesos independientes.
 */
@Component({
  selector: 'mc-admin',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  private readonly api = inject(MediCitaService);
  private readonly fb = inject(FormBuilder);

  readonly EstadoMedico = EstadoMedico;

  readonly resumen = signal<ResumenOperativo | null>(null);
  readonly medicos = signal<Medico[]>([]);
  readonly pacientes = signal<Paciente[]>([]);
  readonly especialidades = signal<Especialidad[]>([]);
  readonly sucursales = signal<Sucursal[]>([]);

  readonly seccion = signal<Seccion>('resumen');
  readonly semana = signal<string | undefined>(undefined);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly aviso = signal<string | null>(null);
  readonly mostrandoFormulario = signal(false);
  readonly guardando = signal(false);

  readonly formularioMedico = this.fb.nonNullable.group({
    cedula: ['', [Validators.required, Validators.minLength(11)]],
    nombre: ['', Validators.required],
    apellido: ['', Validators.required],
    correo: ['', [Validators.required, Validators.email]],
    telefono: [''],
    contrasena: ['', [Validators.required, Validators.minLength(8)]],
    especialidadId: ['', Validators.required],
    sucursalId: ['', Validators.required],
    exequatur: ['', Validators.required],
    consultorio: [''],
    duracionCitaMinutos: [30, [Validators.required, Validators.min(5)]],
  });

  /** Altura relativa de cada barra del gráfico de citas por día. */
  readonly maximoDiario = computed(() =>
    Math.max(1, ...(this.resumen()?.citasPorDia ?? []).map((d) => d.citas))
  );

  ngOnInit(): void {
    this.cargarResumen();

    this.api.especialidades().subscribe({ next: (l) => this.especialidades.set(l) });
    this.api.sucursales().subscribe({ next: (l) => this.sucursales.set(l) });
  }

  cambiarSeccion(seccion: Seccion): void {
    this.seccion.set(seccion);
    this.aviso.set(null);

    if (seccion === 'medicos' && this.medicos().length === 0) this.cargarMedicos();
    if (seccion === 'pacientes' && this.pacientes().length === 0) this.buscarPacientes('');
  }

  semanaAnterior(): void {
    this.semana.set(sumarDias(this.resumen()?.desde ?? hoyIso(), -7));
    this.cargarResumen();
  }

  semanaSiguiente(): void {
    this.semana.set(sumarDias(this.resumen()?.desde ?? hoyIso(), 7));
    this.cargarResumen();
  }

  buscarPacientes(texto: string): void {
    this.api.pacientes(texto.trim() || undefined).subscribe({
      next: (lista) => this.pacientes.set(lista),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  cambiarEstado(medico: Medico, estado: string): void {
    this.api.cambiarEstadoMedico(medico.id, Number(estado) as EstadoMedico).subscribe({
      next: () => {
        this.aviso.set(`Estado de ${medico.nombreCompleto} actualizado.`);
        this.cargarMedicos();
        this.cargarResumen();
      },
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  crearMedico(): void {
    if (this.formularioMedico.invalid) {
      this.formularioMedico.markAllAsTouched();
      return;
    }

    const datos = this.formularioMedico.getRawValue();
    this.guardando.set(true);
    this.error.set(null);

    this.api
      .crearMedico({
        ...datos,
        telefono: datos.telefono || null,
        consultorio: datos.consultorio || null,
      })
      .subscribe({
        next: (medico) => {
          this.guardando.set(false);
          this.mostrandoFormulario.set(false);
          this.formularioMedico.reset({ duracionCitaMinutos: 30 });
          this.aviso.set(`${medico.nombreCompleto} quedó registrado. Falta publicarle horarios.`);
          this.cargarMedicos();
          this.cargarResumen();
        },
        error: (e) => {
          this.guardando.set(false);
          this.error.set(mensajeDeError(e, 'No se pudo registrar el médico.'));
        },
      });
  }

  exportar(): void {
    const datos = this.resumen();
    if (!datos) return;

    this.api.exportarCitas(datos.desde, datos.hasta).subscribe({
      next: (blob) => {
        const enlace = document.createElement('a');
        enlace.href = URL.createObjectURL(blob);
        enlace.download = `citas-${datos.desde}-${datos.hasta}.csv`;
        enlace.click();
        URL.revokeObjectURL(enlace.href);
      },
      error: (e) => this.error.set(mensajeDeError(e, 'No se pudo exportar el archivo.')),
    });
  }

  altura(citas: number): number {
    return Math.max(6, Math.round((citas / this.maximoDiario()) * 100));
  }

  hora(iso: string | null): string {
    return iso ? iso.substring(11, 16) : '—';
  }

  fechaLarga(iso: string): string {
    return fechaLarga(iso);
  }

  private cargarResumen(): void {
    this.cargando.set(true);

    this.api.resumenOperativo(this.semana()).subscribe({
      next: (datos) => {
        this.resumen.set(datos);
        this.cargando.set(false);
      },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  private cargarMedicos(): void {
    this.api.todosLosMedicos().subscribe({
      next: (lista) => this.medicos.set(lista),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }
}
