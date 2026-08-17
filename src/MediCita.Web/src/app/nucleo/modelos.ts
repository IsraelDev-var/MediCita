/** Contratos que devuelve la API REST de MediCita. */

export type Rol = 'Paciente' | 'Medico' | 'Administrador';

export enum EstadoCita {
  Pendiente = 1,
  Confirmada = 2,
  Atendida = 3,
  Cancelada = 4,
  NoAsistio = 5,
}

export enum EstadoCupo {
  Disponible = 1,
  Ocupado = 2,
}

export enum EstadoMedico {
  Activo = 1,
  DeLicencia = 2,
  Inactivo = 3,
}

export enum EstadoNotificacion {
  Pendiente = 1,
  Enviada = 2,
  Fallida = 3,
  Anulada = 4,
}

export interface Usuario {
  id: string;
  cedula: string;
  nombre: string;
  apellido: string;
  nombreCompleto: string;
  correo: string;
  telefono: string | null;
  rol: number;
  rolNombre: Rol;
}

export interface RespuestaAutenticacion {
  token: string;
  expira: string;
  usuario: Usuario;
}

export interface Especialidad {
  id: string;
  nombre: string;
  descripcion: string | null;
  cantidadMedicos: number;
}

export interface Sucursal {
  id: string;
  nombre: string;
  direccion: string | null;
  telefono: string | null;
}

export interface Medico {
  id: string;
  nombreCompleto: string;
  especialidadId: string;
  especialidad: string;
  exequatur: string;
  sucursalId: string;
  sucursal: string;
  consultorio: string | null;
  duracionCitaMinutos: number;
  estado: EstadoMedico;
  estadoNombre: string;
  resumenHorario: string;
  cuposSemanales: number;
}

export interface DiaDisponible {
  fecha: string;
  diaCorto: string;
  dia: number;
  cuposLibres: number;
  cerrado: boolean;
}

export interface Cupo {
  inicio: string;
  fin: string;
  estado: EstadoCupo;
  esDeLaManana: boolean;
  hora: string;
}

export interface Disponibilidad {
  medicoId: string;
  medico: string;
  especialidad: string;
  desde: string;
  hasta: string;
  fechaSeleccionada: string;
  dias: DiaDisponible[];
  cupos: Cupo[];
}

export interface Cita {
  id: string;
  codigo: string;
  inicio: string;
  fin: string;
  duracionMinutos: number;
  estado: EstadoCita;
  estadoNombre: string;
  medicoId: string;
  medico: string;
  especialidad: string;
  sucursal: string;
  consultorio: string | null;
  pacienteId: string;
  paciente: string;
  correoPaciente: string;
  motivoConsulta: string | null;
  notaConsulta: string | null;
  recordatorioProgramado: string | null;
  estadoRecordatorio: EstadoNotificacion | null;
}

export interface CitaAgenda {
  id: string;
  codigo: string;
  inicio: string;
  duracionMinutos: number;
  pacienteId: string;
  paciente: string;
  edadPaciente: number | null;
  cedulaPaciente: string;
  alergias: string | null;
  tipoVisita: string;
  estado: EstadoCita;
  estadoNombre: string;
  motivoConsulta: string | null;
  notaConsulta: string | null;
  ultimaVisita: string | null;
}

export interface EspacioAgenda {
  inicio: string;
  fin: string;
  etiqueta: string;
}

export interface AgendaDelDia {
  fecha: string;
  medicoId: string;
  medico: string;
  citas: CitaAgenda[];
  espacios: EspacioAgenda[];
  atendidasHoy: number;
  totalDelDia: number;
  cuposLibres: number;
  ausenciasDelMes: number;
}

export interface IndicadorDiario {
  fecha: string;
  diaCorto: string;
  citas: number;
}

export interface MedicoOperativo {
  id: string;
  nombre: string;
  especialidad: string;
  cuposSemanales: number;
  ocupacion: number;
  estado: EstadoMedico;
  estadoNombre: string;
}

export interface Actividad {
  momento: string;
  categoria: number;
  descripcion: string;
}

export interface EstadoSistema {
  api: string;
  ultimoCicloWorker: string | null;
  colaSmtpPendiente: number;
  baseDeDatosConectada: boolean;
}

export interface ResumenOperativo {
  desde: string;
  hasta: string;
  citasDeLaSemana: number;
  variacionSemanaAnterior: number;
  porcentajeAusentismo: number;
  variacionAusentismo: number;
  ocupacionDeCupos: number;
  cuposPublicados: number;
  recordatoriosEnviados: number;
  recordatoriosEnCola: number;
  citasPorDia: IndicadorDiario[];
  medicosActivos: MedicoOperativo[];
  estadoSistema: EstadoSistema;
  actividadReciente: Actividad[];
}

export interface Paciente {
  id: string;
  cedula: string;
  nombreCompleto: string;
  correo: string;
  telefono: string | null;
  edad: number | null;
  alergias: string | null;
  activo: boolean;
  citasTotales: number;
}
