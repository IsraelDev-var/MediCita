using MediCita.Domain.Citas;
using MediCita.Domain.Notificaciones;

namespace MediCita.Application.Dtos;

public sealed record SolicitudAgendarCita(Guid MedicoId, DateTime Inicio, string? MotivoConsulta);

public sealed record SolicitudReprogramarCita(DateTime NuevoInicio, Guid? MedicoId = null);

public sealed record SolicitudCancelarCita(string? Motivo);

public sealed record SolicitudAtenderCita(string? NotaConsulta);

/// <summary>Cita como la ve el paciente en "Mis citas" y en el modal de confirmación.</summary>
public sealed record CitaDto(
    Guid Id,
    string Codigo,
    DateTime Inicio,
    DateTime Fin,
    int DuracionMinutos,
    EstadoCita Estado,
    string EstadoNombre,
    Guid MedicoId,
    string Medico,
    string Especialidad,
    string Sucursal,
    string? Consultorio,
    Guid PacienteId,
    string Paciente,
    string CorreoPaciente,
    string? MotivoConsulta,
    string? NotaConsulta,
    DateTime? RecordatorioProgramado,
    EstadoNotificacion? EstadoRecordatorio);

/// <summary>Fila de la agenda diaria del médico (mockup 05).</summary>
public sealed record CitaAgendaDto(
    Guid Id,
    string Codigo,
    DateTime Inicio,
    int DuracionMinutos,
    Guid PacienteId,
    string Paciente,
    int? EdadPaciente,
    string CedulaPaciente,
    string? Alergias,
    string TipoVisita,
    EstadoCita Estado,
    string EstadoNombre,
    string? MotivoConsulta,
    string? NotaConsulta,
    DateTime? UltimaVisita);

/// <summary>Bloque libre o de descanso que la agenda dibuja entre citas.</summary>
public sealed record EspacioAgendaDto(DateTime Inicio, DateTime Fin, string Etiqueta);

public sealed record AgendaDelDiaDto(
    DateOnly Fecha,
    Guid MedicoId,
    string Medico,
    IReadOnlyList<CitaAgendaDto> Citas,
    IReadOnlyList<EspacioAgendaDto> Espacios,
    int AtendidasHoy,
    int TotalDelDia,
    int CuposLibres,
    int AusenciasDelMes);
