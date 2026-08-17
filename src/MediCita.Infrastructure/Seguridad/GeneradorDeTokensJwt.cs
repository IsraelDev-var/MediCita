using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediCita.Application.Abstracciones;
using MediCita.Domain.Usuarios;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediCita.Infrastructure.Seguridad;

/// <summary>
/// Emite el token JWT con el identificador y el rol del usuario. La API lo valida
/// en cada petición y las políticas de autorización leen el rol de aquí.
/// </summary>
public sealed class GeneradorDeTokensJwt : IGeneradorDeTokens
{
    public const string ClaimAccion = "medicita:accion";
    public const string ClaimCita = "medicita:cita";

    private readonly OpcionesJwt _opciones;

    public GeneradorDeTokensJwt(IOptions<OpcionesJwt> opciones)
    {
        _opciones = opciones.Value;

        if (string.IsNullOrWhiteSpace(_opciones.Clave) || _opciones.Clave.Length < 32)
            throw new InvalidOperationException(
                "La clave JWT no está configurada o es demasiado corta (mínimo 32 caracteres). " +
                "Defínala en Jwt:Clave (user-secrets o variables de entorno).");
    }

    public TokenEmitido Generar(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(_opciones.MinutosDeVigencia);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        return new TokenEmitido(Escribir(claims, expira), expira);
    }

    /// <summary>
    /// Token corto y acotado a una acción y una cita: es lo que viaja en los
    /// botones del correo de recordatorio.
    /// </summary>
    public string GenerarEnlaceDeAccion(Usuario usuario, string accion, Guid citaId, TimeSpan vigencia)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new(ClaimAccion, accion),
            new(ClaimCita, citaId.ToString())
        };

        return Escribir(claims, DateTime.UtcNow.Add(vigencia));
    }

    private string Escribir(IEnumerable<Claim> claims, DateTime expira)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Clave));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expira,
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
