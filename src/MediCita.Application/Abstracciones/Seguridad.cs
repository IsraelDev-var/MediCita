using MediCita.Domain.Usuarios;

namespace MediCita.Application.Abstracciones;

/// <summary>Hash y verificación de contraseñas; la implementación vive en infraestructura.</summary>
public interface IHasheadorDeContrasenas
{
    string Hashear(string contrasena);

    bool Verificar(string contrasena, string hash);
}

/// <summary>Emite el token JWT con el rol del usuario, tal como describe el escenario 1.</summary>
public interface IGeneradorDeTokens
{
    TokenEmitido Generar(Usuario usuario);

    /// <summary>Enlace firmado de un solo uso para las acciones del correo de recordatorio.</summary>
    string GenerarEnlaceDeAccion(Usuario usuario, string accion, Guid citaId, TimeSpan vigencia);
}

public sealed record TokenEmitido(string Token, DateTime Expira);

/// <summary>Identidad de quien hace la petición actual; la API la resuelve desde el JWT.</summary>
public interface IUsuarioActual
{
    Guid? Id { get; }

    RolUsuario? Rol { get; }

    bool EsAdministrador => Rol == RolUsuario.Administrador;
}
