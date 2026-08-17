namespace MediCita.Domain.Citas;

/// <summary>
/// Observador de los cambios de estado de una cita (patrón Observer, sección 3.1
/// del documento de arquitectura). La capa de aplicación registra las
/// implementaciones y las notifica dentro de la misma transacción del cambio.
/// </summary>
public interface ICitaObservador
{
    Task AlCambiarEstadoAsync(CambioDeEstadoCita cambio, CancellationToken cancelacion = default);
}
