using MediCita.Domain.Comun;

namespace MediCita.Domain.Agenda;

/// <summary>
/// Franja de atención de un médico en un día de la semana (por ejemplo, lunes de
/// 08:00 a 12:00). El almuerzo no se modela como un bloque especial: es el hueco
/// entre dos franjas, y por eso nunca genera cupos agendables.
/// </summary>
public sealed class Horario : EntidadBase
{
    private Horario() { }

    internal Horario(Guid medicoId, DayOfWeek dia, TimeOnly horaInicio, TimeOnly horaFin, int duracionCupoMinutos)
    {
        if (horaFin <= horaInicio)
            throw new ExcepcionDeDominio("La hora de fin debe ser posterior a la hora de inicio.");

        if (duracionCupoMinutos is < 5 or > 240)
            throw new ExcepcionDeDominio("La duración del cupo debe estar entre 5 y 240 minutos.");

        MedicoId = medicoId;
        Dia = dia;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
        DuracionCupoMinutos = duracionCupoMinutos;
        Activo = true;
    }

    public Guid MedicoId { get; private set; }
    public DayOfWeek Dia { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }
    public int DuracionCupoMinutos { get; private set; }
    public bool Activo { get; private set; }

    public int CantidadDeCupos =>
        (int)((HoraFin - HoraInicio).TotalMinutes / DuracionCupoMinutos);

    public void Suspender() => Activo = false;

    public void Reactivar() => Activo = true;

    public bool SeSolapaCon(Horario otro) =>
        otro.Dia == Dia && otro.HoraInicio < HoraFin && HoraInicio < otro.HoraFin;

    /// <summary>
    /// Genera los instantes de inicio de cada cupo de la franja para una fecha
    /// concreta. Es la fuente de verdad de la disponibilidad: el servicio de
    /// aplicación resta de aquí las citas ya tomadas y los bloqueos.
    /// </summary>
    public IEnumerable<DateTime> GenerarCupos(DateOnly fecha)
    {
        if (!Activo || fecha.DayOfWeek != Dia)
            yield break;

        var inicio = fecha.ToDateTime(HoraInicio);
        var fin = fecha.ToDateTime(HoraFin);

        for (var cupo = inicio; cupo.AddMinutes(DuracionCupoMinutos) <= fin; cupo = cupo.AddMinutes(DuracionCupoMinutos))
            yield return cupo;
    }
}
