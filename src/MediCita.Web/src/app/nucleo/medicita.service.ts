import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AgendaDelDia,
  Cita,
  Disponibilidad,
  Especialidad,
  EstadoMedico,
  Medico,
  Paciente,
  ResumenOperativo,
  Sucursal,
} from './modelos';

/** Único punto de acceso al contrato REST; las pantallas solo llaman a estos métodos. */
@Injectable({ providedIn: 'root' })
export class MediCitaService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.urlApi;

  // --- Catálogos ---------------------------------------------------------------

  especialidades(): Observable<Especialidad[]> {
    return this.http.get<Especialidad[]>(`${this.api}/especialidades`);
  }

  sucursales(): Observable<Sucursal[]> {
    return this.http.get<Sucursal[]>(`${this.api}/sucursales`);
  }

  medicos(especialidadId?: string, sucursalId?: string): Observable<Medico[]> {
    let parametros = new HttpParams();
    if (especialidadId) parametros = parametros.set('especialidadId', especialidadId);
    if (sucursalId) parametros = parametros.set('sucursalId', sucursalId);

    return this.http.get<Medico[]>(`${this.api}/medicos`, { params: parametros });
  }

  // --- Disponibilidad y citas --------------------------------------------------

  disponibilidad(medicoId: string, fecha?: string): Observable<Disponibilidad> {
    const parametros = fecha ? new HttpParams().set('fecha', fecha) : undefined;
    return this.http.get<Disponibilidad>(`${this.api}/disponibilidad/${medicoId}`, { params: parametros });
  }

  agendar(medicoId: string, inicio: string, motivoConsulta: string | null): Observable<Cita> {
    return this.http.post<Cita>(`${this.api}/citas`, { medicoId, inicio, motivoConsulta });
  }

  misCitas(): Observable<Cita[]> {
    return this.http.get<Cita[]>(`${this.api}/citas`);
  }

  cita(id: string): Observable<Cita> {
    return this.http.get<Cita>(`${this.api}/citas/${id}`);
  }

  reprogramar(id: string, nuevoInicio: string, medicoId?: string): Observable<Cita> {
    return this.http.put<Cita>(`${this.api}/citas/${id}/reprogramar`, { nuevoInicio, medicoId });
  }

  confirmar(id: string): Observable<Cita> {
    return this.http.post<Cita>(`${this.api}/citas/${id}/confirmar`, {});
  }

  cancelar(id: string, motivo: string | null): Observable<Cita> {
    return this.http.post<Cita>(`${this.api}/citas/${id}/cancelar`, { motivo });
  }

  // --- Agenda del médico -------------------------------------------------------

  agenda(fecha?: string): Observable<AgendaDelDia> {
    const parametros = fecha ? new HttpParams().set('fecha', fecha) : undefined;
    return this.http.get<AgendaDelDia>(`${this.api}/agenda`, { params: parametros });
  }

  atender(citaId: string, notaConsulta: string | null): Observable<Cita> {
    return this.http.post<Cita>(`${this.api}/agenda/citas/${citaId}/atender`, { notaConsulta });
  }

  registrarAusencia(citaId: string): Observable<Cita> {
    return this.http.post<Cita>(`${this.api}/agenda/citas/${citaId}/ausencia`, {});
  }

  registrarNota(citaId: string, notaConsulta: string | null): Observable<Cita> {
    return this.http.put<Cita>(`${this.api}/agenda/citas/${citaId}/nota`, { notaConsulta });
  }

  // --- Administración ----------------------------------------------------------

  resumenOperativo(semana?: string): Observable<ResumenOperativo> {
    const parametros = semana ? new HttpParams().set('semana', semana) : undefined;
    return this.http.get<ResumenOperativo>(`${this.api}/admin/resumen`, { params: parametros });
  }

  pacientes(busqueda?: string): Observable<Paciente[]> {
    const parametros = busqueda ? new HttpParams().set('busqueda', busqueda) : undefined;
    return this.http.get<Paciente[]>(`${this.api}/admin/pacientes`, { params: parametros });
  }

  todosLosMedicos(): Observable<Medico[]> {
    return this.http.get<Medico[]>(`${this.api}/medicos`, {
      params: new HttpParams().set('soloActivos', false),
    });
  }

  crearMedico(datos: {
    cedula: string;
    nombre: string;
    apellido: string;
    correo: string;
    telefono: string | null;
    contrasena: string;
    especialidadId: string;
    sucursalId: string;
    exequatur: string;
    consultorio: string | null;
    duracionCitaMinutos: number;
  }): Observable<Medico> {
    return this.http.post<Medico>(`${this.api}/admin/medicos`, datos);
  }

  cambiarEstadoMedico(id: string, estado: EstadoMedico): Observable<Medico> {
    return this.http.put<Medico>(`${this.api}/admin/medicos/${id}/estado`, { estado });
  }

  /** Descarga del botón "Exportar CSV" del panel. */
  exportarCitas(desde: string, hasta: string): Observable<Blob> {
    const parametros = new HttpParams().set('desde', desde).set('hasta', hasta);
    return this.http.get(`${this.api}/admin/citas.csv`, { params: parametros, responseType: 'blob' });
  }
}
