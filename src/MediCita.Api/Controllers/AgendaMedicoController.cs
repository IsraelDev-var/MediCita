using MediCita.Api.Seguridad;
using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Application.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCita.Api.Controllers;

/// <summary>
/// Agenda diaria del médico (mockup 05). El token de rol Médico limita todo a su
/// propia agenda: el identificador nunca viaja por la URL.
/// </summary>
[ApiController]
[Route("api/agenda")]
[Authorize(Policy = Politicas.Medico)]
public sealed class AgendaMedicoController : ControllerBase
{
    private readonly ServicioAgendaMedico _servicio;
    private readonly IUsuarioActual _usuarioActual;

    public AgendaMedicoController(ServicioAgendaMedico servicio, IUsuarioActual usuarioActual)
    {
        _servicio = servicio;
        _usuarioActual = usuarioActual;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AgendaDelDiaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgendaDelDiaDto>> Dia(
        [FromQuery] DateOnly? fecha, CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerDelDiaAsync(MedicoId, fecha, cancelacion));

    [HttpPost("citas/{id:guid}/atender")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CitaDto>> Atender(
        Guid id, SolicitudAtenderCita solicitud, CancellationToken cancelacion) =>
        Ok(await _servicio.MarcarAtendidaAsync(id, MedicoId, solicitud.NotaConsulta, cancelacion));

    [HttpPost("citas/{id:guid}/ausencia")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CitaDto>> RegistrarAusencia(Guid id, CancellationToken cancelacion) =>
        Ok(await _servicio.RegistrarAusenciaAsync(id, MedicoId, cancelacion));

    [HttpPut("citas/{id:guid}/nota")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CitaDto>> RegistrarNota(
        Guid id, SolicitudAtenderCita solicitud, CancellationToken cancelacion) =>
        Ok(await _servicio.RegistrarNotaAsync(id, MedicoId, solicitud.NotaConsulta, cancelacion));

    private Guid MedicoId => _usuarioActual.Id
        ?? throw new UnauthorizedAccessException("El token no contiene el identificador del médico.");
}
