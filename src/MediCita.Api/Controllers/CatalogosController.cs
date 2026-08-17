using MediCita.Application.Dtos;
using MediCita.Application.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCita.Api.Controllers;

/// <summary>Pasos 1 y 2 del agendamiento: especialidad, sucursal y médico.</summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class CatalogosController : ControllerBase
{
    private readonly ServicioCatalogos _servicio;

    public CatalogosController(ServicioCatalogos servicio) => _servicio = servicio;

    [HttpGet("especialidades")]
    [ProducesResponseType(typeof(IReadOnlyList<EspecialidadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EspecialidadDto>>> Especialidades(CancellationToken cancelacion) =>
        Ok(await _servicio.ListarEspecialidadesAsync(cancelacion));

    [HttpGet("sucursales")]
    [ProducesResponseType(typeof(IReadOnlyList<SucursalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SucursalDto>>> Sucursales(CancellationToken cancelacion) =>
        Ok(await _servicio.ListarSucursalesAsync(cancelacion));

    [HttpGet("medicos")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicoDto>>> Medicos(
        [FromQuery] Guid? especialidadId,
        [FromQuery] Guid? sucursalId,
        [FromQuery] bool soloActivos = true,
        CancellationToken cancelacion = default) =>
        Ok(await _servicio.ListarMedicosAsync(especialidadId, sucursalId, soloActivos, cancelacion));

    [HttpGet("medicos/{id:guid}")]
    [ProducesResponseType(typeof(MedicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicoDto>> Medico(Guid id, CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerMedicoAsync(id, cancelacion));

    [HttpGet("medicos/{id:guid}/horarios")]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HorarioDto>>> Horarios(Guid id, CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerHorariosAsync(id, cancelacion));
}

/// <summary>Paso 3: disponibilidad en vivo contra el horario del médico.</summary>
[ApiController]
[Route("api/disponibilidad")]
[Authorize]
public sealed class DisponibilidadController : ControllerBase
{
    private readonly ServicioDisponibilidad _servicio;

    public DisponibilidadController(ServicioDisponibilidad servicio) => _servicio = servicio;

    [HttpGet("{medicoId:guid}")]
    [ProducesResponseType(typeof(DisponibilidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisponibilidadDto>> Semana(
        Guid medicoId, [FromQuery] DateOnly? fecha, CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerSemanaAsync(medicoId, fecha, cancelacion));
}
