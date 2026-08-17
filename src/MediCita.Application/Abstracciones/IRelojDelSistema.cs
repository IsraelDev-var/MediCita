namespace MediCita.Application.Abstracciones;

/// <summary>
/// Fuente única de la hora. Se inyecta para que las pruebas puedan fijar el
/// instante y verificar reglas como "no agendar en el pasado".
/// </summary>
public interface IRelojDelSistema
{
    /// <summary>Hora local de la clínica.</summary>
    DateTime Ahora { get; }

    DateOnly Hoy => DateOnly.FromDateTime(Ahora);
}
