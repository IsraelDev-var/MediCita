using System.Security.Claims;
using MediCita.Application.Abstracciones;
using MediCita.Domain.Usuarios;

namespace MediCita.Api.Seguridad;

/// <summary>
/// Lee del token JWT quién está haciendo la petición. La capa de aplicación solo
/// ve la interfaz, así que no depende de ASP.NET Core.
/// </summary>
public sealed class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _acceso;

    public UsuarioActual(IHttpContextAccessor acceso) => _acceso = acceso;

    public Guid? Id
    {
        get
        {
            var valor = _acceso.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(valor, out var id) ? id : null;
        }
    }

    public RolUsuario? Rol
    {
        get
        {
            var valor = _acceso.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<RolUsuario>(valor, out var rol) ? rol : null;
        }
    }
}

/// <summary>Nombres de las políticas de autorización, para no repetir cadenas sueltas.</summary>
public static class Politicas
{
    public const string Paciente = nameof(RolUsuario.Paciente);
    public const string Medico = nameof(RolUsuario.Medico);
    public const string Administrador = nameof(RolUsuario.Administrador);
}
