using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;

namespace MediCita.Application.Abstracciones;

/// <summary>
/// Patrón Repository: la capa de aplicación habla con estas interfaces y nunca
/// con Entity Framework. La infraestructura las implementa, invirtiendo la
/// dependencia como describe la vista de desarrollo.
/// </summary>
public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancelacion = default);

    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion = default);

    Task<bool> ExisteCedulaAsync(string cedula, CancellationToken cancelacion = default);
}

public interface IPacienteRepositorio
{
    Task<Paciente?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Paciente>> ListarAsync(string? busqueda = null, CancellationToken cancelacion = default);

    Task<int> ContarAsync(CancellationToken cancelacion = default);

    void Agregar(Paciente paciente);
}

public interface IMedicoRepositorio
{
    /// <summary>Trae el médico con sus horarios y bloqueos: es lo que necesita el cálculo de cupos.</summary>
    Task<Medico?> ObtenerConAgendaAsync(Guid id, CancellationToken cancelacion = default);

    Task<Medico?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Medico>> ListarAsync(
        Guid? especialidadId = null,
        Guid? sucursalId = null,
        bool soloActivos = true,
        CancellationToken cancelacion = default);

    void Agregar(Medico medico);
}

public interface IEspecialidadRepositorio
{
    Task<Especialidad?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Especialidad>> ListarAsync(bool soloActivas = true, CancellationToken cancelacion = default);

    void Agregar(Especialidad especialidad);
}

public interface ISucursalRepositorio
{
    Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken cancelacion = default);

    void Agregar(Sucursal sucursal);
}

public interface ICitaRepositorio
{
    Task<Cita?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Cita con paciente, médico, especialidad y sucursal cargados.</summary>
    Task<Cita?> ObtenerCompletaAsync(Guid id, CancellationToken cancelacion = default);

    Task<Cita?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Cita>> ObtenerDelPacienteAsync(Guid pacienteId, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Cita>> ObtenerDelMedicoEnRangoAsync(
        Guid medicoId, DateTime desde, DateTime hasta, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Cita>> ObtenerEnRangoAsync(DateTime desde, DateTime hasta, CancellationToken cancelacion = default);

    /// <summary>Verificación de integridad: ¿ya hay una cita viva en ese cupo del médico?</summary>
    Task<bool> ExisteCupoOcupadoAsync(
        Guid medicoId, DateTime inicio, Guid? excluyendoCitaId = null, CancellationToken cancelacion = default);

    /// <summary>Evita que el mismo paciente reserve dos citas a la misma hora.</summary>
    Task<bool> PacienteTieneCitaEnAsync(
        Guid pacienteId, DateTime inicio, Guid? excluyendoCitaId = null, CancellationToken cancelacion = default);

    /// <summary>Correlativo legible del año, con el formato "2026-0731".</summary>
    Task<string> SiguienteCodigoAsync(int anio, CancellationToken cancelacion = default);

    void Agregar(Cita cita);
}

public interface INotificacionRepositorio
{
    /// <summary>Recordatorios que ya deben salir; el worker las procesa por lotes.</summary>
    Task<IReadOnlyList<Notificacion>> ObtenerDespachablesAsync(
        DateTime hasta, int limite = 50, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Notificacion>> ObtenerDeCitaAsync(Guid citaId, CancellationToken cancelacion = default);

    Task<int> ContarPorEstadoAsync(
        EstadoNotificacion estado, DateTime? desde = null, CancellationToken cancelacion = default);

    void Agregar(Notificacion notificacion);
}

public interface IBitacoraRepositorio
{
    Task<IReadOnlyList<RegistroActividad>> ObtenerRecientesAsync(int cantidad = 10, CancellationToken cancelacion = default);

    void Agregar(RegistroActividad registro);
}

/// <summary>Único punto de contacto entre el proceso de la API y el del worker.</summary>
public interface ILatidoRepositorio
{
    Task<LatidoDelWorker?> ObtenerUltimoAsync(CancellationToken cancelacion = default);

    void Agregar(LatidoDelWorker latido);
}
