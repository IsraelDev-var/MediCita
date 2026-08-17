using MediCita.Domain.Comun;

namespace MediCita.Application.Dtos;

public sealed record IndicadorDiarioDto(DateOnly Fecha, string DiaCorto, int Citas);

/// <summary>Fila de la tabla "Médicos activos" del panel: cupos publicados y ocupación de la semana.</summary>
public sealed record MedicoOperativoDto(
    Guid Id,
    string Nombre,
    string Especialidad,
    int CuposSemanales,
    double Ocupacion,
    Domain.Usuarios.EstadoMedico Estado,
    string EstadoNombre);

public sealed record ActividadDto(DateTime Momento, CategoriaActividad Categoria, string Descripcion);

/// <summary>
/// Bloque "Estado del sistema" del panel: expone API, worker y cola SMTP como
/// procesos independientes, tal como los describe la vista de procesos.
/// </summary>
public sealed record EstadoSistemaDto(
    string Api,
    DateTime? UltimoCicloWorker,
    int ColaSmtpPendiente,
    bool BaseDeDatosConectada);

/// <summary>Resumen operativo semanal del mockup 06.</summary>
public sealed record ResumenOperativoDto(
    DateOnly Desde,
    DateOnly Hasta,
    int CitasDeLaSemana,
    double VariacionSemanaAnterior,
    double PorcentajeAusentismo,
    double VariacionAusentismo,
    double OcupacionDeCupos,
    int CuposPublicados,
    int RecordatoriosEnviados,
    int RecordatoriosEnCola,
    IReadOnlyList<IndicadorDiarioDto> CitasPorDia,
    IReadOnlyList<MedicoOperativoDto> MedicosActivos,
    EstadoSistemaDto EstadoSistema,
    IReadOnlyList<ActividadDto> ActividadReciente);

public sealed record PacienteDto(
    Guid Id,
    string Cedula,
    string NombreCompleto,
    string Correo,
    string? Telefono,
    int? Edad,
    string? Alergias,
    bool Activo,
    int CitasTotales);
