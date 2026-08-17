using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Application.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCita.Api.Controllers;

/// <summary>Pantalla 01: acceso y registro. La misma sirve a los tres roles.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AutenticacionController : ControllerBase
{
    private readonly ServicioAutenticacion _servicio;
    private readonly IUsuarioActual _usuarioActual;

    public AutenticacionController(ServicioAutenticacion servicio, IUsuarioActual usuarioActual)
    {
        _servicio = servicio;
        _usuarioActual = usuarioActual;
    }

    /// <summary>Devuelve el token JWT con el rol; Angular redirige según ese rol.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaAutenticacion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RespuestaAutenticacion>> IniciarSesion(
        SolicitudLogin solicitud, CancellationToken cancelacion) =>
        Ok(await _servicio.IniciarSesionAsync(solicitud, cancelacion));

    /// <summary>Alta de paciente con cédula, correo y teléfono.</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaAutenticacion), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RespuestaAutenticacion>> Registrar(
        SolicitudRegistroPaciente solicitud, CancellationToken cancelacion)
    {
        var respuesta = await _servicio.RegistrarPacienteAsync(solicitud, cancelacion);
        return Created($"/api/auth/yo", respuesta);
    }

    [HttpGet("yo")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Perfil(CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerPerfilAsync(UsuarioId, cancelacion));

    [HttpPut("contacto")]
    [Authorize]
    public async Task<IActionResult> ActualizarContacto(
        ActualizarContactoSolicitud solicitud, CancellationToken cancelacion)
    {
        await _servicio.ActualizarContactoAsync(UsuarioId, solicitud.Correo, solicitud.Telefono, cancelacion);
        return NoContent();
    }

    [HttpPut("contrasena")]
    [Authorize]
    public async Task<IActionResult> CambiarContrasena(
        CambiarContrasenaSolicitud solicitud, CancellationToken cancelacion)
    {
        await _servicio.CambiarContrasenaAsync(UsuarioId, solicitud.Actual, solicitud.Nueva, cancelacion);
        return NoContent();
    }

    private Guid UsuarioId => _usuarioActual.Id
        ?? throw new UnauthorizedAccessException("El token no contiene el identificador del usuario.");
}

public sealed record ActualizarContactoSolicitud(string Correo, string? Telefono);

public sealed record CambiarContrasenaSolicitud(string Actual, string Nueva);
