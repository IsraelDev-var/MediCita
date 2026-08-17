import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { fechaLarga, hoyIso, sumarDias } from '../../nucleo/fechas';
import { mensajeDeError } from '../../nucleo/interceptores';
import { MediCitaService } from '../../nucleo/medicita.service';
import { AgendaDelDia, CitaAgenda, EstadoCita } from '../../nucleo/modelos';

interface FilaAgenda {
  hora: string;
  cita?: CitaAgenda;
  espacio?: string;
}

/**
 * Pantalla 05. La agenda del día y el panel del paciente en consulta; el cambio
 * de estado se registra sin salir de la pantalla.
 */
@Component({
  selector: 'mc-agenda-medico',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './agenda-medico.component.html',
  styleUrl: './agenda-medico.component.scss',
})
export class AgendaMedicoComponent implements OnInit {
  private readonly api = inject(MediCitaService);

  readonly agenda = signal<AgendaDelDia | null>(null);
  readonly fecha = signal<string | undefined>(undefined);
  readonly seleccionadaId = signal<string | null>(null);
  readonly nota = signal<string>('');
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly seleccionada = computed(() => {
    const id = this.seleccionadaId();
    const citas = this.agenda()?.citas ?? [];
    return citas.find((c) => c.id === id) ?? citas.find((c) => this.esOperable(c)) ?? citas[0] ?? null;
  });

  /** Mezcla citas y bloques libres en una sola línea de tiempo ordenada. */
  readonly filas = computed<FilaAgenda[]>(() => {
    const datos = this.agenda();
    if (!datos) return [];

    const filas: FilaAgenda[] = [
      ...datos.citas.map((cita) => ({ hora: cita.inicio, cita })),
      ...datos.espacios.map((espacio) => ({ hora: espacio.inicio, espacio: espacio.etiqueta })),
    ];

    return filas.sort((a, b) => a.hora.localeCompare(b.hora));
  });

  ngOnInit(): void {
    this.cargar();
  }

  irA(dias: number): void {
    const actual = this.agenda()?.fecha ?? hoyIso();
    this.fecha.set(sumarDias(actual, dias));
    this.cargar();
  }

  irAHoy(): void {
    this.fecha.set(hoyIso());
    this.cargar();
  }

  seleccionar(cita: CitaAgenda): void {
    this.seleccionadaId.set(cita.id);
    this.nota.set(cita.notaConsulta ?? '');
  }

  marcarAtendida(): void {
    const cita = this.seleccionada();
    if (!cita) return;

    this.guardando.set(true);

    this.api.atender(cita.id, this.nota().trim() || null).subscribe({
      next: () => this.recargarTrasCambio(),
      error: (e) => this.fallo(e, 'No se pudo marcar la cita como atendida.'),
    });
  }

  registrarAusencia(): void {
    const cita = this.seleccionada();
    if (!cita) return;

    this.guardando.set(true);

    this.api.registrarAusencia(cita.id).subscribe({
      next: () => this.recargarTrasCambio(),
      error: (e) => this.fallo(e, 'No se pudo registrar la ausencia.'),
    });
  }

  guardarNota(): void {
    const cita = this.seleccionada();
    if (!cita) return;

    this.guardando.set(true);

    this.api.registrarNota(cita.id, this.nota().trim() || null).subscribe({
      next: () => this.recargarTrasCambio(),
      error: (e) => this.fallo(e, 'No se pudo guardar la nota.'),
    });
  }

  esOperable(cita: CitaAgenda): boolean {
    return cita.estado === EstadoCita.Pendiente || cita.estado === EstadoCita.Confirmada;
  }

  claseChip(estado: EstadoCita): string {
    switch (estado) {
      case EstadoCita.Confirmada:
        return 'chip chip-confirmada';
      case EstadoCita.Atendida:
        return 'chip chip-atendida';
      case EstadoCita.NoAsistio:
        return 'chip chip-noasistio';
      default:
        return 'chip chip-pendiente';
    }
  }

  hora(iso: string): string {
    return iso.substring(11, 16);
  }

  fechaLarga(iso: string): string {
    return fechaLarga(iso);
  }

  private cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.api.agenda(this.fecha()).subscribe({
      next: (datos) => {
        this.agenda.set(datos);
        this.cargando.set(false);

        const actual = this.seleccionada();
        this.nota.set(actual?.notaConsulta ?? '');
      },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  private recargarTrasCambio(): void {
    this.guardando.set(false);
    this.cargar();
  }

  private fallo(error: unknown, respaldo: string): void {
    this.guardando.set(false);
    this.error.set(mensajeDeError(error, respaldo));
  }
}
