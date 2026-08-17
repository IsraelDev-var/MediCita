import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { fechaCorta, fechaLarga, hora12, mesDe, sumarDias } from '../../nucleo/fechas';
import { mensajeDeError } from '../../nucleo/interceptores';
import { MediCitaService } from '../../nucleo/medicita.service';
import { Cita, Cupo, Disponibilidad, Especialidad, EstadoCupo, Medico, Sucursal } from '../../nucleo/modelos';

/**
 * Pantallas 02 y 03. Los tres pasos viven en una sola vista para cumplir la meta
 * de usabilidad: agendar en menos de un minuto sin recargar la página.
 *
 * La misma pantalla se reutiliza para reprogramar: con ?reprogramar=<id> el médico
 * queda fijo y al confirmar se llama al endpoint de reprogramación.
 */
@Component({
  selector: 'mc-agendar',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './agendar.component.html',
  styleUrl: './agendar.component.scss',
})
export class AgendarComponent implements OnInit {
  private readonly api = inject(MediCitaService);
  private readonly ruta = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly EstadoCupo = EstadoCupo;

  readonly especialidades = signal<Especialidad[]>([]);
  readonly sucursales = signal<Sucursal[]>([]);
  readonly medicos = signal<Medico[]>([]);
  readonly disponibilidad = signal<Disponibilidad | null>(null);

  readonly especialidadId = signal<string>('');
  readonly sucursalId = signal<string>('');
  readonly medicoSeleccionado = signal<Medico | null>(null);
  readonly cupoSeleccionado = signal<Cupo | null>(null);
  readonly motivo = signal<string>('');

  readonly cargandoCupos = signal(false);
  readonly enviando = signal(false);
  readonly error = signal<string | null>(null);
  readonly citaConfirmada = signal<Cita | null>(null);

  /** Cita que se está moviendo, cuando la pantalla se usa para reprogramar. */
  readonly reprogramandoId = signal<string | null>(null);

  readonly cuposManana = computed(() =>
    (this.disponibilidad()?.cupos ?? []).filter((c) => c.esDeLaManana)
  );

  readonly cuposTarde = computed(() =>
    (this.disponibilidad()?.cupos ?? []).filter((c) => !c.esDeLaManana)
  );

  readonly puedeConfirmar = computed(
    () => this.medicoSeleccionado() !== null && this.cupoSeleccionado() !== null && !this.enviando()
  );

  ngOnInit(): void {
    this.api.especialidades().subscribe({
      next: (lista) => {
        this.especialidades.set(lista);
        this.prepararDesdeParametros();
      },
      error: (e) => this.error.set(mensajeDeError(e)),
    });

    this.api.sucursales().subscribe({
      next: (lista) => this.sucursales.set(lista),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  // --- Pasos 1 y 2 --------------------------------------------------------------

  cambiarEspecialidad(id: string): void {
    this.especialidadId.set(id);
    this.medicoSeleccionado.set(null);
    this.disponibilidad.set(null);
    this.cupoSeleccionado.set(null);
    this.cargarMedicos();
  }

  cambiarSucursal(id: string): void {
    this.sucursalId.set(id);
    this.medicoSeleccionado.set(null);
    this.disponibilidad.set(null);
    this.cupoSeleccionado.set(null);
    this.cargarMedicos();
  }

  elegirMedico(medico: Medico): void {
    this.medicoSeleccionado.set(medico);
    this.cupoSeleccionado.set(null);
    this.cargarDisponibilidad();
  }

  // --- Paso 3 -------------------------------------------------------------------

  elegirDia(fecha: string): void {
    this.cupoSeleccionado.set(null);
    this.cargarDisponibilidad(fecha);
  }

  semanaAnterior(): void {
    const desde = this.disponibilidad()?.desde;
    if (desde) this.elegirDia(sumarDias(desde, -7));
  }

  semanaSiguiente(): void {
    const desde = this.disponibilidad()?.desde;
    if (desde) this.elegirDia(sumarDias(desde, 7));
  }

  elegirCupo(cupo: Cupo): void {
    if (cupo.estado === EstadoCupo.Ocupado) return;
    this.cupoSeleccionado.set(cupo);
  }

  // --- Confirmación --------------------------------------------------------------

  confirmar(): void {
    const medico = this.medicoSeleccionado();
    const cupo = this.cupoSeleccionado();
    if (!medico || !cupo) return;

    this.enviando.set(true);
    this.error.set(null);

    const reprogramarId = this.reprogramandoId();

    const peticion = reprogramarId
      ? this.api.reprogramar(reprogramarId, cupo.inicio, medico.id)
      : this.api.agendar(medico.id, cupo.inicio, this.motivo().trim() || null);

    peticion.subscribe({
      next: (cita) => {
        this.citaConfirmada.set(cita);
        this.enviando.set(false);
      },
      error: (e) => {
        this.enviando.set(false);
        this.error.set(mensajeDeError(e, 'No se pudo agendar la cita.'));

        // Si el cupo se ocupó entre la selección y el envío, se vuelve al paso 3
        // con la disponibilidad recién consultada.
        if (e?.status === 409) {
          this.cupoSeleccionado.set(null);
          this.cargarDisponibilidad(this.disponibilidad()?.fechaSeleccionada);
        }
      },
    });
  }

  cerrarConfirmacion(): void {
    this.citaConfirmada.set(null);
    this.router.navigate(['/citas']);
  }

  /** Descarga un archivo .ics para agregar la cita al calendario del paciente. */
  agregarAlCalendario(): void {
    const cita = this.citaConfirmada();
    if (!cita) return;

    const comoUtc = (iso: string) =>
      new Date(iso).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '');

    const ics = [
      'BEGIN:VCALENDAR',
      'VERSION:2.0',
      'PRODID:-//MediCita//ES',
      'BEGIN:VEVENT',
      `UID:${cita.id}@medicita.do`,
      `DTSTAMP:${comoUtc(new Date().toISOString())}`,
      `DTSTART:${comoUtc(cita.inicio)}`,
      `DTEND:${comoUtc(cita.fin)}`,
      `SUMMARY:Cita con ${cita.medico} (${cita.especialidad})`,
      `LOCATION:${cita.sucursal}${cita.consultorio ? ' - Consultorio ' + cita.consultorio : ''}`,
      `DESCRIPTION:Cita ${cita.codigo}. Llega 15 minutos antes con tu cédula.`,
      'END:VEVENT',
      'END:VCALENDAR',
    ].join('\r\n');

    const enlace = document.createElement('a');
    enlace.href = URL.createObjectURL(new Blob([ics], { type: 'text/calendar' }));
    enlace.download = `cita-${cita.codigo}.ics`;
    enlace.click();
    URL.revokeObjectURL(enlace.href);
  }

  // --- Presentación --------------------------------------------------------------

  hora(iso: string): string {
    return iso.substring(11, 16);
  }

  hora12(iso: string): string {
    return hora12(iso);
  }

  mes(iso: string): string {
    return mesDe(iso);
  }

  fechaLarga(iso: string): string {
    return fechaLarga(iso);
  }

  fechaCorta(iso: string): string {
    return fechaCorta(iso);
  }

  // --- Carga de datos -------------------------------------------------------------

  private cargarMedicos(): void {
    this.api.medicos(this.especialidadId() || undefined, this.sucursalId() || undefined).subscribe({
      next: (lista) => this.medicos.set(lista),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  private cargarDisponibilidad(fecha?: string): void {
    const medico = this.medicoSeleccionado();
    if (!medico) return;

    this.cargandoCupos.set(true);

    this.api.disponibilidad(medico.id, fecha).subscribe({
      next: (datos) => {
        this.disponibilidad.set(datos);
        this.cargandoCupos.set(false);
      },
      error: (e) => {
        this.cargandoCupos.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  /** Soporta llegar aquí desde "Reprogramar" o desde el enlace del correo. */
  private prepararDesdeParametros(): void {
    const parametros = this.ruta.snapshot.queryParamMap;
    const reprogramar = parametros.get('reprogramar');
    const medicoId = parametros.get('medicoId');

    if (reprogramar) this.reprogramandoId.set(reprogramar);

    if (!medicoId) {
      this.cargarMedicos();
      return;
    }

    this.api.medicos().subscribe({
      next: (lista) => {
        this.medicos.set(lista);
        const medico = lista.find((m) => m.id === medicoId);

        if (medico) {
          this.especialidadId.set(medico.especialidadId);
          this.sucursalId.set(medico.sucursalId);
          this.elegirMedico(medico);
        }
      },
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }
}
