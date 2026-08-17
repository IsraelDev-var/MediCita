using System.Text;
using MediCita.Domain.Notificaciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediCita.Infrastructure.Notificaciones;

/// <summary>
/// Estrategia de canal para desarrollo: en lugar de salir a un servidor SMTP,
/// escribe cada correo como archivo .eml, que se abre con cualquier cliente de
/// correo. Permite ver el recordatorio real sin credenciales de SendGrid.
/// </summary>
public sealed class CanalCorreoArchivo : IEstrategiaDeCanal
{
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<CanalCorreoArchivo> _log;

    public CanalCorreoArchivo(IOptions<OpcionesCorreo> opciones, ILogger<CanalCorreoArchivo> log)
    {
        _opciones = opciones.Value;
        _log = log;
    }

    public CanalNotificacion Canal => CanalNotificacion.Correo;

    public async Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancelacion = default)
    {
        var carpeta = Path.IsPathRooted(_opciones.CarpetaSalida)
            ? _opciones.CarpetaSalida
            : Path.Combine(AppContext.BaseDirectory, _opciones.CarpetaSalida);

        Directory.CreateDirectory(carpeta);

        var archivo = Path.Combine(
            carpeta,
            $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Sanear(mensaje.Destinatario)}.eml");

        await File.WriteAllTextAsync(archivo, ArmarEml(mensaje), Encoding.UTF8, cancelacion);

        _log.LogInformation(
            "Recordatorio escrito para {Destinatario} en {Archivo}", mensaje.Destinatario, archivo);
    }

    private string ArmarEml(MensajeNotificacion mensaje)
    {
        var eml = new StringBuilder();
        eml.AppendLine($"From: {_opciones.RemitenteNombre} <{_opciones.RemitenteCorreo}>");
        eml.AppendLine($"To: {mensaje.Destinatario}");
        eml.AppendLine($"Subject: {mensaje.Asunto}");
        eml.AppendLine($"Date: {DateTime.Now:R}");
        eml.AppendLine("MIME-Version: 1.0");
        eml.AppendLine("Content-Type: text/html; charset=utf-8");
        eml.AppendLine();
        eml.AppendLine(mensaje.CuerpoHtml ?? mensaje.CuerpoTexto);

        return eml.ToString();
    }

    private static string Sanear(string destinatario)
    {
        var limpio = new string(destinatario.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        return limpio.Length > 40 ? limpio[..40] : limpio;
    }
}
