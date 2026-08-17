using System.Globalization;
using System.Net;
using MediCita.Domain.Citas;

namespace MediCita.Domain.Notificaciones;

/// <summary>
/// Correo HTML del recordatorio (mockup 07): una sola columna de 600 px de ancho
/// fijo, sin depender de imágenes, para que se vea igual en clientes de escritorio
/// y móviles.
/// </summary>
public static class PlantillaCorreoRecordatorio
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private const string Navy = "#22303F";
    private const string Azul = "#5B7FA6";
    private const string Gris = "#6B7580";
    private const string Linea = "#DCDEDF";

    public static string Asunto(Cita cita) =>
        $"Recordatorio: tu cita es mañana a las {Hora(cita.FechaHoraInicio)}";

    public static string CuerpoTexto(Cita cita)
    {
        var paciente = cita.Paciente?.Nombre ?? "paciente";
        var medico = NombreMedico(cita);
        var lugar = Lugar(cita);

        return $"""
            Hola {paciente}, tu cita es mañana.

            {FechaLarga(cita.FechaHoraInicio)} a las {Hora(cita.FechaHoraInicio)}
            Médico: {medico}
            Especialidad: {cita.Medico?.Especialidad?.Nombre ?? "—"}
            Lugar: {lugar}

            Llega 15 minutos antes con tu cédula. Si no puedes asistir, reprograma o
            cancela para liberar el cupo a otro paciente.

            Correo automático de MediCita · Cita {cita.Codigo} · No respondas a esta dirección.
            """;
    }

    public static string CuerpoHtml(Cita cita, string? urlConfirmar, string? urlReprogramar)
    {
        var paciente = Escapar(cita.Paciente?.Nombre ?? "paciente");
        var medico = Escapar(NombreMedico(cita));
        var especialidad = Escapar(cita.Medico?.Especialidad?.Nombre ?? "—");
        var lugar = Escapar(Lugar(cita));
        var fecha = Escapar(FechaLarga(cita.FechaHoraInicio).ToUpper(Cultura));
        var hora = Escapar(Hora(cita.FechaHoraInicio));

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Recordatorio de cita</title>
            </head>
            <body style="margin:0;padding:24px 0;background:#F1F1F0;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:{Navy};">
              <table role="presentation" width="600" cellpadding="0" cellspacing="0" align="center"
                     style="width:600px;max-width:100%;background:#FFFFFF;border:1px solid {Linea};border-collapse:collapse;">
                <tr>
                  <td style="background:{Navy};padding:28px 32px;">
                    <div style="font-size:24px;font-weight:600;color:#FFFFFF;">MediCita</div>
                    <div style="font-size:11px;letter-spacing:2px;color:#AEBAC6;margin-top:6px;">RECORDATORIO DE CITA</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px 32px 8px 32px;">
                    <h1 style="margin:0;font-size:24px;font-weight:600;">Hola {paciente}, tu cita es mañana.</h1>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 32px 0 32px;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                           style="border:1px solid {Linea};border-collapse:collapse;">
                      <tr>
                        <td style="padding:20px 24px;">
                          <div style="font-size:11px;letter-spacing:2px;color:{Azul};">{fecha}</div>
                          <div style="font-size:34px;font-weight:600;margin:6px 0 18px 0;">{hora}</div>
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
                            <tr>
                              <td style="padding:8px 0;border-bottom:1px solid {Linea};color:{Gris};">Médico</td>
                              <td style="padding:8px 0;border-bottom:1px solid {Linea};text-align:right;font-weight:600;">{medico}</td>
                            </tr>
                            <tr>
                              <td style="padding:8px 0;border-bottom:1px solid {Linea};color:{Gris};">Especialidad</td>
                              <td style="padding:8px 0;border-bottom:1px solid {Linea};text-align:right;font-weight:600;">{especialidad}</td>
                            </tr>
                            <tr>
                              <td style="padding:8px 0;color:{Gris};">Lugar</td>
                              <td style="padding:8px 0;text-align:right;font-weight:600;">{lugar}</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td style="padding:22px 32px 0 32px;font-size:14px;line-height:22px;">
                    Llega 15 minutos antes con tu cédula. Si no puedes asistir, reprograma o
                    cancela para liberar el cupo a otro paciente.
                  </td>
                </tr>
                {BotonesHtml(urlConfirmar, urlReprogramar)}
                <tr>
                  <td style="padding:24px 32px 30px 32px;border-top:1px solid {Linea};font-size:12px;color:{Gris};">
                    Correo automático de MediCita · Cita {Escapar(cita.Codigo)} · No respondas a esta dirección.
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BotonesHtml(string? urlConfirmar, string? urlReprogramar)
    {
        if (string.IsNullOrWhiteSpace(urlConfirmar) && string.IsNullOrWhiteSpace(urlReprogramar))
            return string.Empty;

        var confirmar = string.IsNullOrWhiteSpace(urlConfirmar)
            ? string.Empty
            : $"""
               <td style="padding-right:12px;">
                 <a href="{Escapar(urlConfirmar)}"
                    style="display:block;padding:14px 24px;background:{Azul};color:#FFFFFF;text-decoration:none;font-weight:600;font-size:14px;text-align:center;">Confirmar asistencia</a>
               </td>
               """;

        var reprogramar = string.IsNullOrWhiteSpace(urlReprogramar)
            ? string.Empty
            : $"""
               <td>
                 <a href="{Escapar(urlReprogramar)}"
                    style="display:block;padding:14px 24px;border:1px solid {Linea};color:{Navy};text-decoration:none;font-weight:600;font-size:14px;text-align:center;">Reprogramar</a>
               </td>
               """;

        return $"""
            <tr>
              <td style="padding:24px 32px 8px 32px;">
                <table role="presentation" cellpadding="0" cellspacing="0" style="border-collapse:collapse;width:100%;">
                  <tr>{confirmar}{reprogramar}</tr>
                </table>
              </td>
            </tr>
            """;
    }

    private static string NombreMedico(Cita cita) =>
        cita.Medico is null ? "—" : $"Dr(a). {cita.Medico.NombreCompleto}";

    private static string Lugar(Cita cita)
    {
        var sede = cita.Sucursal?.Nombre;
        var consultorio = cita.Consultorio;

        return (sede, consultorio) switch
        {
            (null, null) => "Consultar en recepción",
            (null, _) => $"Consultorio {consultorio}",
            (_, null) => sede!,
            _ => $"{sede} · Consultorio {consultorio}"
        };
    }

    private static string FechaLarga(DateTime fecha) =>
        fecha.ToString("dddd d 'de' MMMM 'de' yyyy", Cultura);

    /// <summary>
    /// Se arma a mano en vez de usar "tt": según la versión de ICU, es-DO produce
    /// "a. m." con espacios especiales y el diseño pide "a.m.".
    /// </summary>
    internal static string Hora(DateTime fecha) =>
        $"{fecha.ToString("hh\\:mm", Cultura)} {(fecha.Hour < 12 ? "a.m." : "p.m.")}";

    private static string Escapar(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);
}
