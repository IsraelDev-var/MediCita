using MediCita.Application.Abstracciones;
using MediCita.Domain.Citas;
using MediCita.Domain.Notificaciones;

namespace MediCita.Application.Citas.Observadores;

/// <summary>
/// Observador que mantiene sincronizado el recordatorio con el estado de la cita:
/// lo programa cuando la cita nace o se mueve, y lo anula cuando la cita deja de
/// ocupar el cupo. Es el paso (6) del escenario "Agendar una cita".
/// </summary>
public sealed class ProgramadorDeRecordatorios : ICitaObservador
{
    private readonly INotificacionRepositorio _notificaciones;
    private readonly IRelojDelSistema _reloj;

    public ProgramadorDeRecordatorios(INotificacionRepositorio notificaciones, IRelojDelSistema reloj)
    {
        _notificaciones = notificaciones;
        _reloj = reloj;
    }

    public async Task AlCambiarEstadoAsync(CambioDeEstadoCita cambio, CancellationToken cancelacion = default)
    {
        var cita = cambio.Cita;

        if (cambio.DejaDeOcuparCupo)
        {
            await AnularPendientesAsync(cita.Id, cancelacion);
            return;
        }

        // Solo se programa al agendar o al reprogramar (la cita vuelve a Pendiente).
        if (cambio.EstadoNuevo != EstadoCita.Pendiente)
            return;

        await AnularPendientesAsync(cita.Id, cancelacion);

        if (cita.Paciente is null)
            return;

        _notificaciones.Agregar(NotificacionCorreo.ProgramarRecordatorio(cita, _reloj.Ahora));
    }

    private async Task AnularPendientesAsync(Guid citaId, CancellationToken cancelacion)
    {
        var existentes = await _notificaciones.ObtenerDeCitaAsync(citaId, cancelacion);

        foreach (var notificacion in existentes.Where(n => n.EstaPendiente))
            notificacion.Anular();
    }
}
