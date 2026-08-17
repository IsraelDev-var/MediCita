using MediCita.Application.Abstracciones;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;

namespace MediCita.Application.Servicios;

/// <summary>Configuración del envío de recordatorios; se lee desde appsettings.</summary>
public sealed class OpcionesRecordatorio
{
    public const string Seccion = "Recordatorios";

    /// <summary>Cada cuántos minutos despierta el worker (escenario 2: cada 5 minutos).</summary>
    public int MinutosEntreCiclos { get; set; } = 5;

    /// <summary>Cuántas notificaciones procesa por ciclo.</summary>
    public int TamanoDelLote { get; set; } = 50;

    /// <summary>Después de estos intentos fallidos la notificación deja de reintentarse.</summary>
    public int IntentosMaximos { get; set; } = 5;

    /// <summary>Base de los enlaces "Confirmar asistencia" y "Reprogramar" del correo.</summary>
    public string UrlAplicacion { get; set; } = "http://localhost:4200";

    public CanalNotificacion CanalPredeterminado { get; set; } = CanalNotificacion.Correo;
}

public sealed record ResultadoDelCiclo(int Procesadas, int Enviadas, int Fallidas, DateTime Momento)
{
    public static ResultadoDelCiclo Vacio(DateTime momento) => new(0, 0, 0, momento);
}

/// <summary>
/// Escenario 2 del documento. Toma los recordatorios cuya hora ya llegó, arma el
/// mensaje con la estrategia del canal y marca cada envío para no duplicarlo. Un
/// fallo no detiene el lote: la notificación queda fallida y se reintenta en el
/// próximo ciclo, sin que la API se entere.
/// </summary>
public sealed class ServicioRecordatorios
{
    private readonly INotificacionRepositorio _notificaciones;
    private readonly ILatidoRepositorio _latidos;
    private readonly IBitacoraRepositorio _bitacora;
    private readonly ISelectorDeCanal _canales;
    private readonly IGeneradorDeTokens _tokens;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IRelojDelSistema _reloj;
    private readonly OpcionesRecordatorio _opciones;

    public ServicioRecordatorios(
        INotificacionRepositorio notificaciones,
        ILatidoRepositorio latidos,
        IBitacoraRepositorio bitacora,
        ISelectorDeCanal canales,
        IGeneradorDeTokens tokens,
        IUnidadDeTrabajo unidad,
        IRelojDelSistema reloj,
        OpcionesRecordatorio opciones)
    {
        _notificaciones = notificaciones;
        _latidos = latidos;
        _bitacora = bitacora;
        _canales = canales;
        _tokens = tokens;
        _unidad = unidad;
        _reloj = reloj;
        _opciones = opciones;
    }

    public async Task<ResultadoDelCiclo> EjecutarCicloAsync(CancellationToken cancelacion = default)
    {
        var ahora = _reloj.Ahora;

        var pendientes = await _notificaciones.ObtenerDespachablesAsync(ahora, _opciones.TamanoDelLote, cancelacion);

        var enviadas = 0;
        var fallidas = 0;
        var procesadas = 0;

        foreach (var notificacion in pendientes)
        {
            cancelacion.ThrowIfCancellationRequested();

            // La cita pudo cancelarse entre la programación y el envío.
            if (notificacion.Cita is null || !notificacion.Cita.OcupaCupo)
            {
                notificacion.Anular();
                continue;
            }

            if (notificacion.Intentos >= _opciones.IntentosMaximos)
            {
                notificacion.Anular();
                continue;
            }

            if (!_canales.EstaDisponible(notificacion.Canal))
                continue;

            PrepararEnlaces(notificacion);

            procesadas++;

            if (await notificacion.EnviarAsync(_canales.Para(notificacion.Canal), cancelacion))
                enviadas++;
            else
                fallidas++;
        }

        if (enviadas > 0)
        {
            _bitacora.Agregar(new RegistroActividad(
                CategoriaActividad.Recordatorio,
                enviadas == 1 ? "1 recordatorio enviado" : $"{enviadas} recordatorios enviados",
                ahora));
        }

        _latidos.Agregar(new LatidoDelWorker(ahora, procesadas, enviadas, fallidas));
        await _unidad.GuardarCambiosAsync(cancelacion);

        return new ResultadoDelCiclo(procesadas, enviadas, fallidas, ahora);
    }

    /// <summary>
    /// Los botones del correo llevan un enlace firmado que abre la app ya
    /// autenticada, tal como indica la nota del mockup 07.
    /// </summary>
    private void PrepararEnlaces(Notificacion notificacion)
    {
        if (notificacion is not NotificacionCorreo correo)
            return;

        var paciente = correo.Cita?.Paciente;
        if (paciente is null)
            return;

        var vigencia = TimeSpan.FromHours(48);
        var baseUrl = _opciones.UrlAplicacion.TrimEnd('/');

        correo.EstablecerEnlaces(
            $"{baseUrl}/citas/{correo.CitaId}/confirmar?t={_tokens.GenerarEnlaceDeAccion(paciente, "confirmar", correo.CitaId, vigencia)}",
            $"{baseUrl}/citas/{correo.CitaId}/reprogramar?t={_tokens.GenerarEnlaceDeAccion(paciente, "reprogramar", correo.CitaId, vigencia)}");
    }
}
