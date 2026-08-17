using System.Net;
using System.Net.Mail;
using MediCita.Domain.Notificaciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediCita.Infrastructure.Notificaciones;

/// <summary>
/// Estrategia de canal de producción: entrega el correo al servicio SMTP externo
/// (SendGrid en la vista física). Cambiar de proveedor es cambiar appsettings.
/// </summary>
public sealed class CanalCorreoSmtp : IEstrategiaDeCanal
{
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<CanalCorreoSmtp> _log;

    public CanalCorreoSmtp(IOptions<OpcionesCorreo> opciones, ILogger<CanalCorreoSmtp> log)
    {
        _opciones = opciones.Value;
        _log = log;
    }

    public CanalNotificacion Canal => CanalNotificacion.Correo;

    public async Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancelacion = default)
    {
        using var cliente = new SmtpClient(_opciones.Servidor, _opciones.Puerto)
        {
            EnableSsl = _opciones.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
        {
            cliente.UseDefaultCredentials = false;
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Clave);
        }

        using var correo = new MailMessage
        {
            From = new MailAddress(_opciones.RemitenteCorreo, _opciones.RemitenteNombre),
            Subject = mensaje.Asunto,
            Body = mensaje.CuerpoHtml ?? mensaje.CuerpoTexto,
            IsBodyHtml = mensaje.CuerpoHtml is not null
        };

        correo.To.Add(mensaje.Destinatario);

        await cliente.SendMailAsync(correo, cancelacion);

        _log.LogInformation("Recordatorio enviado por SMTP a {Destinatario}", mensaje.Destinatario);
    }
}
