namespace MediCita.Domain.Notificaciones;

/// <summary>
/// Mensaje ya armado y listo para salir por un canal. Es el contrato entre la
/// notificación (qué se dice) y la estrategia de canal (cómo se entrega).
/// </summary>
public sealed record MensajeNotificacion(
    CanalNotificacion Canal,
    string Destinatario,
    string Asunto,
    string CuerpoTexto,
    string? CuerpoHtml = null);

/// <summary>
/// Estrategia de envío (patrón Strategy). Cada canal —correo hoy, SMS o WhatsApp
/// mañana— implementa esta interfaz sin obligar a tocar el código que la invoca.
/// </summary>
public interface IEstrategiaDeCanal
{
    CanalNotificacion Canal { get; }

    Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancelacion = default);
}
