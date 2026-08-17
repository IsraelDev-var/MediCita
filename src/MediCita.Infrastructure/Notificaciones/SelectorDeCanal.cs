using MediCita.Application.Abstracciones;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;

namespace MediCita.Infrastructure.Notificaciones;

/// <summary>
/// Punto donde se resuelve la estrategia de envío. Si hay más de una registrada
/// para el mismo canal gana la última, que es la que la configuración eligió.
/// </summary>
public sealed class SelectorDeCanal : ISelectorDeCanal
{
    private readonly Dictionary<CanalNotificacion, IEstrategiaDeCanal> _estrategias;

    public SelectorDeCanal(IEnumerable<IEstrategiaDeCanal> estrategias)
    {
        _estrategias = new Dictionary<CanalNotificacion, IEstrategiaDeCanal>();

        foreach (var estrategia in estrategias)
            _estrategias[estrategia.Canal] = estrategia;
    }

    public bool EstaDisponible(CanalNotificacion canal) => _estrategias.ContainsKey(canal);

    public IEstrategiaDeCanal Para(CanalNotificacion canal) =>
        _estrategias.TryGetValue(canal, out var estrategia)
            ? estrategia
            : throw new ExcepcionDeDominio($"No hay un canal de envío registrado para {canal}.");
}
