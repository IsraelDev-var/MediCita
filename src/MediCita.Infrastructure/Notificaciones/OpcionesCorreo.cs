namespace MediCita.Infrastructure.Notificaciones;

/// <summary>Cómo salen los correos. En desarrollo se escriben a disco; en producción van por SMTP.</summary>
public enum ModoDeCorreo
{
    /// <summary>Guarda cada mensaje como archivo .eml para poder abrirlo y revisarlo.</summary>
    Archivo = 0,

    /// <summary>Entrega real contra un servidor SMTP (SendGrid u otro).</summary>
    Smtp = 1
}

public sealed class OpcionesCorreo
{
    public const string Seccion = "Correo";

    public ModoDeCorreo Modo { get; set; } = ModoDeCorreo.Archivo;

    public string CarpetaSalida { get; set; } = "correos-salida";

    public string Servidor { get; set; } = "localhost";
    public int Puerto { get; set; } = 25;
    public bool UsarSsl { get; set; }
    public string? Usuario { get; set; }
    public string? Clave { get; set; }

    public string RemitenteCorreo { get; set; } = "no-responder@medicita.do";
    public string RemitenteNombre { get; set; } = "MediCita";
}
