using MediCita.Domain.Citas;
using MediCita.Domain.Comun;

namespace MediCita.Domain.Notificaciones;

/// <summary>
/// Notificación entregada por SMS. El mismo <c>Enviar()</c> heredado produce un
/// resultado distinto porque el mensaje se arma en texto plano y recortado a 160
/// caracteres: ese es el polimorfismo que describe la vista lógica.
/// </summary>
public sealed class NotificacionSms : Notificacion
{
    private const int LargoMaximo = 160;

    private NotificacionSms() { }

    private NotificacionSms(Cita cita, string telefono, DateTime fechaProgramada, TipoNotificacion tipo)
        : base(cita, telefono, fechaProgramada, tipo)
    {
    }

    public override CanalNotificacion Canal => CanalNotificacion.Sms;

    public static NotificacionSms ProgramarRecordatorio(Cita cita, DateTime? ahora = null)
    {
        ArgumentNullException.ThrowIfNull(cita);

        var telefono = cita.Paciente?.Telefono
            ?? throw new ExcepcionDeDominio("El paciente no tiene teléfono registrado.");

        var referencia = ahora ?? DateTime.Now;
        var programada = cita.FechaHoraInicio.AddHours(-24);
        if (programada < referencia) programada = referencia;

        return new NotificacionSms(cita, telefono, programada, TipoNotificacion.Recordatorio24Horas);
    }

    public override MensajeNotificacion Construir()
    {
        var cita = Cita ?? throw new ExcepcionDeDominio("No se puede construir el SMS sin la cita asociada.");

        var apellido = cita.Medico?.Apellido ?? "tu médico";
        var hora = PlantillaCorreoRecordatorio.Hora(cita.FechaHoraInicio);
        var texto = $"MediCita: tu cita con Dr(a). {apellido} es mañana a las {hora}. Cita {cita.Codigo}.";

        if (texto.Length > LargoMaximo)
            texto = texto[..LargoMaximo];

        return new MensajeNotificacion(Canal, Destinatario, "Recordatorio de cita", texto);
    }
}
