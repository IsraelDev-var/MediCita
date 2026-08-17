namespace MediCita.Domain.Agenda;

/// <summary>Estados con los que la pantalla de agendamiento pinta cada cupo.</summary>
public enum EstadoCupo
{
    Disponible = 1,
    Ocupado = 2
}

/// <summary>
/// Objeto de valor: un espacio concreto de la agenda de un médico. No se persiste,
/// se calcula al vuelo cruzando los horarios con las citas ya tomadas.
/// </summary>
public sealed record Cupo(DateTime Inicio, int DuracionMinutos, EstadoCupo Estado)
{
    public DateTime Fin => Inicio.AddMinutes(DuracionMinutos);

    public bool EsDeLaManana => Inicio.Hour < 12;

    public static Cupo Disponible(DateTime inicio, int duracion) => new(inicio, duracion, EstadoCupo.Disponible);

    public static Cupo Ocupado(DateTime inicio, int duracion) => new(inicio, duracion, EstadoCupo.Ocupado);
}
