using MediCita.Api.Seguridad;
using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Application.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCita.Api.Controllers;

/// <summary>
/// Citas del paciente: agendar (escenario 1), reprogramar, confirmar y cancelar.
/// </summary>
[ApiController]
[Route("api/citas")]
[Authorize]
public sealed class CitasController : ControllerBase
{
    private readonly ServicioCitas _servicio;
    private readonly IUsuarioActual _usuarioActual;

    public CitasController(ServicioCitas servicio, IUsuarioActual usuarioActual)
    {
        _servicio = servicio;
        _usuarioActual = usuarioActual;
    }

    /// <summary>Crea la cita en estado Pendiente y programa el recordatorio de 24 horas.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Paciente)]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CitaDto>> Agendar(SolicitudAgendarCita solicitud, CancellationToken cancelacion)
    {
        var cita = await _servicio.AgendarAsync(UsuarioId, solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = cita.Id }, cita);
    }

    /// <summary>Listado de "Mis citas": próximas, historial y canceladas.</summary>
    [HttpGet]
    [Authorize(Policy = Politicas.Paciente)]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> Mias(CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerDelPacienteAsync(UsuarioId, cancelacion));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerAsync(id, _usuarioActual, cancelacion));

    /// <summary>Mueve la cita a otro cupo; el anterior queda libre al persistirse.</summary>
    [HttpPut("{id:guid}/reprogramar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CitaDto>> Reprogramar(
        Guid id, SolicitudReprogramarCita solicitud, CancellationToken cancelacion) =>
        Ok(await _servicio.ReprogramarAsync(id, solicitud, _usuarioActual, cancelacion));

    [HttpPost("{id:guid}/confirmar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CitaDto>> Confirmar(Guid id, CancellationToken cancelacion) =>
        Ok(await _servicio.ConfirmarAsync(id, _usuarioActual, cancelacion));

    /// <summary>Cancela la cita y anula el recordatorio pendiente.</summary>
    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CitaDto>> Cancelar(
        Guid id, SolicitudCancelarCita solicitud, CancellationToken cancelacion) =>
        Ok(await _servicio.CancelarAsync(id, solicitud, _usuarioActual, cancelacion));

    private Guid UsuarioId => _usuarioActual.Id
        ?? throw new UnauthorizedAccessException("El token no contiene el identificador del usuario.");
}
