using MediCita.Domain.Citas;

namespace MediCita.Application.Citas;

/// <summary>Sujeto observable del patrón Observer: reparte los cambios de estado de una cita.</summary>
public interface IPublicadorDeCambiosDeCita
{
    Task PublicarAsync(Cita cita, CancellationToken cancelacion = default);
}

/// <summary>
/// Recorre los cambios acumulados por la cita y se los entrega a cada observador
/// registrado, dentro de la misma transacción. Al terminar limpia la lista para
/// que un segundo guardado no vuelva a notificar lo mismo.
/// </summary>
public sealed class PublicadorDeCambiosDeCita : IPublicadorDeCambiosDeCita
{
    private readonly IEnumerable<ICitaObservador> _observadores;

    public PublicadorDeCambiosDeCita(IEnumerable<ICitaObservador> observadores) => _observadores = observadores;

    public async Task PublicarAsync(Cita cita, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(cita);

        var cambios = cita.CambiosDeEstado.ToList();
        cita.LimpiarCambiosDeEstado();

        foreach (var cambio in cambios)
            foreach (var observador in _observadores)
                await observador.AlCambiarEstadoAsync(cambio, cancelacion);
    }
}
