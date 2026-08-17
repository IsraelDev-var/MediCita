namespace MediCita.Infrastructure.Seguridad;

/// <summary>Parámetros del token JWT; la clave se lee de configuración, nunca del código.</summary>
public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = "MediCita.Api";
    public string Audiencia { get; set; } = "MediCita.Web";
    public string Clave { get; set; } = string.Empty;
    public int MinutosDeVigencia { get; set; } = 480;
}
