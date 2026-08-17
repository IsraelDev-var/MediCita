namespace MediCita.Domain.Comun;

/// <summary>
/// Error provocado por una regla de negocio. La API lo traduce a un 400/409
/// con ProblemDetails, de modo que el dominio nunca conoce HTTP.
/// </summary>
public class ExcepcionDeDominio : Exception
{
    public ExcepcionDeDominio(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Conflicto de concurrencia sobre un cupo: dos citas no pueden ocupar el mismo
/// espacio de un médico (requisito de integridad de la arquitectura).
/// </summary>
public sealed class CupoNoDisponibleException : ExcepcionDeDominio
{
    public CupoNoDisponibleException(DateTime inicio)
        : base($"El cupo de las {inicio:hh\\:mm tt} del {inicio:dd/MM/yyyy} ya fue tomado por otro paciente.") { }
}

/// <summary>Se pidió una entidad que no existe.</summary>
public sealed class NoEncontradoException : ExcepcionDeDominio
{
    public NoEncontradoException(string entidad, object clave)
        : base($"No se encontró {entidad} con identificador '{clave}'.") { }
}
