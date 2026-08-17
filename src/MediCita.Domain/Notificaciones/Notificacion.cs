using MediCita.Domain.Citas;
using MediCita.Domain.Comun;

namespace MediCita.Domain.Notificaciones;

/// <summary>
/// Aviso programado para un paciente. Es abstracta a propósito: el método
/// <see cref="EnviarAsync"/> se comporta distinto según el canal concreto
/// (<see cref="NotificacionCorreo"/> o <see cref="NotificacionSms"/>), que es el
/// ejemplo de polimorfismo descrito en la vista lógica.
/// </summary>
public abstract class Notificacion : EntidadBase
{
    protected Notificacion() { }

    protected Notificacion(Cita cita, string destinatario, DateTime fechaProgramada, TipoNotificacion tipo)
    {
        ArgumentNullException.ThrowIfNull(cita);

        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ExcepcionDeDominio("La notificación necesita un destinatario.");

        CitaId = cita.Id;
        Cita = cita;
        Destinatario = destinatario.Trim();
        FechaProgramada = fechaProgramada;
        Tipo = tipo;
        Estado = EstadoNotificacion.Pendiente;
    }

    public Guid CitaId { get; private set; }
    public Cita? Cita { get; private set; }

    public string Destinatario { get; private set; } = string.Empty;
    public DateTime FechaProgramada { get; private set; }
    public TipoNotificacion Tipo { get; private set; }
    public EstadoNotificacion Estado { get; private set; }
    public int Intentos { get; private set; }
    public DateTime? FechaEnvio { get; private set; }
    public string? UltimoError { get; private set; }

    /// <summary>Canal por el que sale esta notificación; lo fija la subclase.</summary>
    public abstract CanalNotificacion Canal { get; }

    public bool EstaPendiente => Estado is EstadoNotificacion.Pendiente or EstadoNotificacion.Fallida;

    /// <summary>Arma el contenido del mensaje según el canal.</summary>
    public abstract MensajeNotificacion Construir();

    /// <summary>
    /// Envía la notificación usando la estrategia del canal. Un fallo no propaga
    /// la excepción: se registra y el worker reintenta en el próximo ciclo, que es
    /// la tolerancia a fallas descrita en el escenario 2.
    /// </summary>
    public virtual async Task<bool> EnviarAsync(IEstrategiaDeCanal estrategia, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(estrategia);

        if (Estado is EstadoNotificacion.Enviada or EstadoNotificacion.Anulada)
            return false;

        if (estrategia.Canal != Canal)
            throw new ExcepcionDeDominio($"La estrategia recibida entrega por {estrategia.Canal} y esta notificación es de {Canal}.");

        Intentos++;

        try
        {
            await estrategia.EnviarAsync(Construir(), cancelacion);
            Estado = EstadoNotificacion.Enviada;
            FechaEnvio = DateTime.Now;
            UltimoError = null;
            return true;
        }
        catch (Exception ex)
        {
            Estado = EstadoNotificacion.Fallida;
            UltimoError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            return false;
        }
    }

    /// <summary>Cancelar o reprogramar la cita deja sin efecto el recordatorio pendiente.</summary>
    public void Anular()
    {
        if (Estado == EstadoNotificacion.Enviada)
            return;

        Estado = EstadoNotificacion.Anulada;
    }

    public void Reprogramar(DateTime fechaProgramada)
    {
        if (Estado == EstadoNotificacion.Enviada)
            throw new ExcepcionDeDominio("No se puede reprogramar una notificación que ya se envió.");

        FechaProgramada = fechaProgramada;
        Estado = EstadoNotificacion.Pendiente;
    }

    /// <summary>Ya se puede despachar: llegó su hora y la cita sigue vigente.</summary>
    public bool EsDespachable(DateTime ahora) => EstaPendiente && FechaProgramada <= ahora;
}
