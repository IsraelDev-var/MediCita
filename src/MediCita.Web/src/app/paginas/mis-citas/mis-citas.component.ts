import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { cuandoFalta, fechaCorta, hora12 } from '../../nucleo/fechas';
import { mensajeDeError } from '../../nucleo/interceptores';
import { MediCitaService } from '../../nucleo/medicita.service';
import { Cita, EstadoCita } from '../../nucleo/modelos';

type Pestana = 'proximas' | 'historial' | 'canceladas';

/** Pantalla 04: próximas citas, historial y cancelaciones. */
@Component({
  selector: 'mc-mis-citas',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './mis-citas.component.html',
  styleUrl: './mis-citas.component.scss',
})
export class MisCitasComponent implements OnInit {
  private readonly api = inject(MediCitaService);
  private readonly router = inject(Router);

  readonly citas = signal<Cita[]>([]);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly pestana = signal<Pestana>('proximas');
  readonly citaACancelar = signal<Cita | null>(null);
  readonly cancelando = signal(false);

  readonly proximas = computed(() =>
    this.citas()
      .filter((c) => c.estado === EstadoCita.Pendiente || c.estado === EstadoCita.Confirmada)
      .sort((a, b) => a.inicio.localeCompare(b.inicio))
  );

  readonly historial = computed(() =>
    this.citas()
      .filter((c) => c.estado === EstadoCita.Atendida || c.estado === EstadoCita.NoAsistio)
      .sort((a, b) => b.inicio.localeCompare(a.inicio))
  );

  readonly canceladas = computed(() =>
    this.citas()
      .filter((c) => c.estado === EstadoCita.Cancelada)
      .sort((a, b) => b.inicio.localeCompare(a.inicio))
  );

  readonly visibles = computed(() => {
    switch (this.pestana()) {
      case 'historial':
        return this.historial();
      case 'canceladas':
        return this.canceladas();
      default:
        return this.proximas();
    }
  });

  ngOnInit(): void {
    this.cargar();
  }

  reprogramar(cita: Cita): void {
    this.router.navigate(['/citas/nueva'], {
      queryParams: { reprogramar: cita.id, medicoId: cita.medicoId },
    });
  }

  pedirCancelacion(cita: Cita): void {
    this.citaACancelar.set(cita);
  }

  confirmarCancelacion(motivo: string): void {
    const cita = this.citaACancelar();
    if (!cita) return;

    this.cancelando.set(true);

    this.api.cancelar(cita.id, motivo.trim() || null).subscribe({
      next: () => {
        this.cancelando.set(false);
        this.citaACancelar.set(null);
        this.cargar();
      },
      error: (e) => {
        this.cancelando.set(false);
        this.citaACancelar.set(null);
        this.error.set(mensajeDeError(e, 'No se pudo cancelar la cita.'));
      },
    });
  }

  claseChip(estado: EstadoCita): string {
    switch (estado) {
      case EstadoCita.Confirmada:
        return 'chip chip-confirmada';
      case EstadoCita.Atendida:
        return 'chip chip-atendida';
      case EstadoCita.Cancelada:
        return 'chip chip-cancelada';
      case EstadoCita.NoAsistio:
        return 'chip chip-noasistio';
      default:
        return 'chip chip-pendiente';
    }
  }

  hora(iso: string): string {
    return iso.substring(11, 16);
  }

  hora12(iso: string): string {
    return hora12(iso);
  }

  fechaCorta(iso: string): string {
    return fechaCorta(iso);
  }

  cuandoFalta(iso: string): string {
    return cuandoFalta(iso);
  }

  private cargar(): void {
    this.cargando.set(true);

    this.api.misCitas().subscribe({
      next: (lista) => {
        this.citas.set(lista);
        this.cargando.set(false);
      },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }
}
