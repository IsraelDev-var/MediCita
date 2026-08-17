namespace MediCita.Domain.Notificaciones;

/// <summary>Canales de envío. Hoy solo se usa correo; SMS queda listo para el futuro.</summary>
public enum CanalNotificacion
{
    Correo = 1,
    Sms = 2
}

/// <summary>Estado del envío de una notificación.</summary>
public enum EstadoNotificacion
{
    Pendiente = 1,
    Enviada = 2,
    Fallida = 3,
    Anulada = 4
}

/// <summary>Motivo por el que se generó la notificación.</summary>
public enum TipoNotificacion
{
    Recordatorio24Horas = 1,
    AvisoDeCancelacion = 2,
    AvisoDeReprogramacion = 3
}
