namespace MediCita.Application.Abstracciones;

/// <summary>
/// Confirma en un solo paso todos los cambios hechos a través de los repositorios.
/// Mantiene la transacción fuera de la capa de aplicación, que solo declara la intención.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);
}
