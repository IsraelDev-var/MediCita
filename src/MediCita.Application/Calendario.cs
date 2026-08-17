namespace MediCita.Application;

/// <summary>Cálculos de calendario compartidos por los servicios.</summary>
public static class Calendario
{
    /// <summary>Lunes de la semana a la que pertenece la fecha.</summary>
    public static DateOnly InicioDeSemana(DateOnly fecha) => fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    /// <summary>
    /// Día que se muestra por omisión cuando el usuario no pide una fecha. En fin
    /// de semana la clínica no atiende, así que se salta al lunes siguiente en vez
    /// de abrir una vista vacía.
    /// </summary>
    public static DateOnly DiaPorDefecto(DateOnly hoy) => hoy.DayOfWeek switch
    {
        DayOfWeek.Saturday => hoy.AddDays(2),
        DayOfWeek.Sunday => hoy.AddDays(1),
        _ => hoy
    };
}
