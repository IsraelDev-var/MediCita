using MediCita.Domain.Notificaciones;
using Microsoft.Extensions.Logging;

namespace MediCita.Infrastructure.Notificaciones;

/// <summary>
/// Segundo canal registrado. Hoy solo deja traza en el log, pero demuestra que
/// agregar un canal nuevo no obliga a tocar ni el worker ni las notificaciones:
/// basta registrar otra estrategia.
/// </summary>
public sealed class CanalSmsSimulado : IEstrategiaDeCanal
{
    private readonly ILogger<CanalSmsSimulado> _log;

    public CanalSmsSimulado(ILogger<CanalSmsSimulado> log) => _log = log;

    public CanalNotificacion Canal => CanalNotificacion.Sms;

    public Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancelacion = default)
    {
        _log.LogInformation("SMS simulado a {Destinatario}: {Texto}", mensaje.Destinatario, mensaje.CuerpoTexto);
        return Task.CompletedTask;
    }
}
