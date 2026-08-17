using MediCita.Domain.Notificaciones;

namespace MediCita.Application.Abstracciones;

/// <summary>
/// Devuelve la estrategia de envío registrada para un canal. Agregar WhatsApp
/// mañana es registrar una implementación más: ni el worker ni las notificaciones
/// cambian (patrón Strategy).
/// </summary>
public interface ISelectorDeCanal
{
    IEstrategiaDeCanal Para(CanalNotificacion canal);

    bool EstaDisponible(CanalNotificacion canal);
}
