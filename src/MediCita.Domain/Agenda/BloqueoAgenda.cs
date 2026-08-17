using MediCita.Domain.Comun;

namespace MediCita.Domain.Agenda;

/// <summary>
/// Excepción puntual sobre el horario regular: licencia, vacaciones, feriado o
/// una reunión. Los cupos que caen dentro del rango no se publican.
/// </summary>
public sealed class BloqueoAgenda : EntidadBase
{
    private BloqueoAgenda() { }

    internal BloqueoAgenda(Guid medicoId, DateTime desde, DateTime hasta, string motivo)
    {
        if (hasta <= desde)
            throw new ExcepcionDeDominio("El fin del bloqueo debe ser posterior a su inicio.");

        MedicoId = medicoId;
        Desde = desde;
        Hasta = hasta;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? "Bloqueo de agenda" : motivo.Trim();
    }

    public Guid MedicoId { get; private set; }
    public DateTime Desde { get; private set; }
    public DateTime Hasta { get; private set; }
    public string Motivo { get; private set; } = string.Empty;

    public bool Cubre(DateTime inicioCupo, int duracionMinutos)
    {
        var finCupo = inicioCupo.AddMinutes(duracionMinutos);
        return inicioCupo < Hasta && Desde < finCupo;
    }
}
