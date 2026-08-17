namespace MediCita.Domain.Citas;

/// <summary>
/// Evento que la <see cref="Cita"/> publica cuando cambia de estado. Es el
/// mensaje que reciben los observadores registrados (patrón Observer).
/// </summary>
public sealed record CambioDeEstadoCita(
    Cita Cita,
    EstadoCita? EstadoAnterior,
    EstadoCita EstadoNuevo,
    string Detalle,
    DateTime Ocurrido)
{
    public bool EsAgendamiento => EstadoAnterior is null;

    public bool DejaDeOcuparCupo =>
        EstadoNuevo is EstadoCita.Cancelada or EstadoCita.Atendida or EstadoCita.NoAsistio;
}
