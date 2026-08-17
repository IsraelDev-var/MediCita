using MediCita.Domain.Usuarios;

namespace MediCita.Application.Dtos;

public sealed record EspecialidadDto(Guid Id, string Nombre, string? Descripcion, int CantidadMedicos);

public sealed record SucursalDto(Guid Id, string Nombre, string? Direccion, string? Telefono);

/// <summary>Tarjeta de médico del paso 2 del agendamiento y fila del panel de administración.</summary>
public sealed record MedicoDto(
    Guid Id,
    string NombreCompleto,
    Guid EspecialidadId,
    string Especialidad,
    string Exequatur,
    Guid SucursalId,
    string Sucursal,
    string? Consultorio,
    int DuracionCitaMinutos,
    EstadoMedico Estado,
    string EstadoNombre,
    string ResumenHorario,
    int CuposSemanales);

public sealed record HorarioDto(
    Guid Id,
    DayOfWeek Dia,
    string DiaNombre,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    int DuracionCupoMinutos,
    int CantidadDeCupos,
    bool Activo);

public sealed record SolicitudNuevoMedico(
    string Cedula,
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    string Contrasena,
    Guid EspecialidadId,
    Guid SucursalId,
    string Exequatur,
    string? Consultorio,
    int DuracionCitaMinutos = 30);

public sealed record SolicitudNuevoHorario(DayOfWeek Dia, TimeOnly HoraInicio, TimeOnly HoraFin);
