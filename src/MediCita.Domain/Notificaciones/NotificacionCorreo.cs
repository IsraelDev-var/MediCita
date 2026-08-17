using MediCita.Domain.Citas;
using MediCita.Domain.Comun;

namespace MediCita.Domain.Notificaciones;

/// <summary>
/// Notificación entregada por correo electrónico. Construye el mensaje HTML
/// completo del mockup 07; es la implementación que se usa hoy en producción.
/// </summary>
public sealed class NotificacionCorreo : Notificacion
{
    private NotificacionCorreo() { }

    private NotificacionCorreo(Cita cita, string destinatario, DateTime fechaProgramada, TipoNotificacion tipo)
        : base(cita, destinatario, fechaProgramada, tipo)
    {
    }

    public override CanalNotificacion Canal => CanalNotificacion.Correo;

    /// <summary>Enlaces firmados que abren la app ya autenticada desde el correo.</summary>
    public string? UrlConfirmar { get; private set; }
    public string? UrlReprogramar { get; private set; }

    /// <summary>
    /// Programa el recordatorio de 24 horas antes. Si la cita es en menos de un día,
    /// queda programado para el instante actual y sale en el próximo ciclo del worker.
    /// </summary>
    public static NotificacionCorreo ProgramarRecordatorio(Cita cita, DateTime? ahora = null)
    {
        ArgumentNullException.ThrowIfNull(cita);

        var correo = cita.Paciente?.Correo
            ?? throw new ExcepcionDeDominio("La cita no tiene el paciente cargado; no se puede programar el recordatorio.");

        var referencia = ahora ?? DateTime.Now;
        var programada = cita.FechaHoraInicio.AddHours(-24);
        if (programada < referencia) programada = referencia;

        return new NotificacionCorreo(cita, correo, programada, TipoNotificacion.Recordatorio24Horas);
    }

    public void EstablecerEnlaces(string? urlConfirmar, string? urlReprogramar)
    {
        UrlConfirmar = string.IsNullOrWhiteSpace(urlConfirmar) ? null : urlConfirmar.Trim();
        UrlReprogramar = string.IsNullOrWhiteSpace(urlReprogramar) ? null : urlReprogramar.Trim();
    }

    public override MensajeNotificacion Construir()
    {
        var cita = Cita ?? throw new ExcepcionDeDominio("No se puede construir el correo sin la cita asociada.");

        return new MensajeNotificacion(
            Canal,
            Destinatario,
            PlantillaCorreoRecordatorio.Asunto(cita),
            PlantillaCorreoRecordatorio.CuerpoTexto(cita),
            PlantillaCorreoRecordatorio.CuerpoHtml(cita, UrlConfirmar, UrlReprogramar));
    }
}
