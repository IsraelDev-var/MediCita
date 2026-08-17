using MediCita.Domain.Comun;

namespace MediCita.Domain.Usuarios;

/// <summary>
/// Clase base de la jerarquía de usuarios (Figura 1). Concentra los datos de
/// identificación y el comportamiento de autenticación que <see cref="Paciente"/>
/// y <see cref="Medico"/> heredan.
/// </summary>
public abstract class Usuario : EntidadBase
{
    // Los atributos se mantienen privados/protegidos: hacia afuera solo se exponen
    // operaciones y propiedades de lectura (ocultación de la información).
    private string _hashContrasena = string.Empty;

    protected Usuario() { }

    protected Usuario(string cedula, string nombre, string apellido, string correo, string? telefono, RolUsuario rol)
    {
        Cedula = NormalizarCedula(cedula);
        Nombre = ExigirTexto(nombre, "el nombre");
        Apellido = ExigirTexto(apellido, "el apellido");
        Correo = NormalizarCorreo(correo);
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Rol = rol;
        Activo = true;
    }

    public string Cedula { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string? Telefono { get; private set; }
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateTime? UltimoAcceso { get; private set; }

    public string NombreCompleto => $"{Nombre} {Apellido}";

    /// <summary>Hash de la contraseña. Solo la infraestructura lo lee para comparar.</summary>
    public string HashContrasena
    {
        get => _hashContrasena;
        private set => _hashContrasena = value;
    }

    public void EstablecerContrasena(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ExcepcionDeDominio("La contraseña no puede quedar vacía.");

        _hashContrasena = hash;
    }

    public void RegistrarAcceso() => UltimoAcceso = DateTime.UtcNow;

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    public void ActualizarContacto(string correo, string? telefono)
    {
        Correo = NormalizarCorreo(correo);
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
    }

    protected static string ExigirTexto(string valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ExcepcionDeDominio($"Debe indicar {campo}.");

        return valor.Trim();
    }

    private static string NormalizarCorreo(string correo)
    {
        var limpio = ExigirTexto(correo, "el correo electrónico").ToLowerInvariant();
        if (!limpio.Contains('@') || limpio.StartsWith('@') || limpio.EndsWith('@'))
            throw new ExcepcionDeDominio($"El correo '{correo}' no tiene un formato válido.");

        return limpio;
    }

    private static string NormalizarCedula(string cedula)
    {
        var digitos = new string(ExigirTexto(cedula, "la cédula").Where(char.IsDigit).ToArray());
        if (digitos.Length != 11)
            throw new ExcepcionDeDominio("La cédula debe tener 11 dígitos.");

        return $"{digitos[..3]}-{digitos[3..10]}-{digitos[10..]}";
    }
}
