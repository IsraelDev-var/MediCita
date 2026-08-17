using System.Text;
using MediCita.Api.Seguridad;
using MediCita.Application.Dtos;
using MediCita.Application.Servicios;
using MediCita.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCita.Api.Controllers;

/// <summary>Panel de administración (mockup 06): indicadores y gestión de la clínica.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Politicas.Administrador)]
public sealed class AdministracionController : ControllerBase
{
    private readonly ServicioAdministracion _servicio;

    public AdministracionController(ServicioAdministracion servicio) => _servicio = servicio;

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenOperativoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumenOperativoDto>> Resumen(
        [FromQuery] DateOnly? semana, CancellationToken cancelacion) =>
        Ok(await _servicio.ObtenerResumenAsync(semana, cancelacion));

    [HttpGet("pacientes")]
    [ProducesResponseType(typeof(IReadOnlyList<PacienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PacienteDto>>> Pacientes(
        [FromQuery] string? busqueda, CancellationToken cancelacion) =>
        Ok(await _servicio.ListarPacientesAsync(busqueda, cancelacion));

    [HttpPost("medicos")]
    [ProducesResponseType(typeof(MedicoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MedicoDto>> CrearMedico(
        SolicitudNuevoMedico solicitud, CancellationToken cancelacion)
    {
        var medico = await _servicio.CrearMedicoAsync(solicitud, cancelacion);
        return Created($"/api/medicos/{medico.Id}", medico);
    }

    [HttpPut("medicos/{id:guid}/estado")]
    [ProducesResponseType(typeof(MedicoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MedicoDto>> CambiarEstado(
        Guid id, CambiarEstadoMedicoSolicitud solicitud, CancellationToken cancelacion) =>
        Ok(await _servicio.CambiarEstadoMedicoAsync(id, solicitud.Estado, cancelacion));

    [HttpPost("medicos/{id:guid}/horarios")]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<HorarioDto>> AgregarHorario(
        Guid id, SolicitudNuevoHorario solicitud, CancellationToken cancelacion)
    {
        var horario = await _servicio.AgregarHorarioAsync(id, solicitud, cancelacion);
        return Created($"/api/medicos/{id}/horarios", horario);
    }

    [HttpDelete("medicos/{id:guid}/horarios/{horarioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SuspenderHorario(Guid id, Guid horarioId, CancellationToken cancelacion)
    {
        await _servicio.SuspenderHorarioAsync(id, horarioId, cancelacion);
        return NoContent();
    }

    [HttpPost("especialidades")]
    [ProducesResponseType(typeof(EspecialidadDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EspecialidadDto>> CrearEspecialidad(
        CrearEspecialidadSolicitud solicitud, CancellationToken cancelacion)
    {
        var especialidad = await _servicio.CrearEspecialidadAsync(solicitud.Nombre, solicitud.Descripcion, cancelacion);
        return Created("/api/especialidades", especialidad);
    }

    [HttpPost("sucursales")]
    [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SucursalDto>> CrearSucursal(
        CrearSucursalSolicitud solicitud, CancellationToken cancelacion)
    {
        var sucursal = await _servicio.CrearSucursalAsync(
            solicitud.Nombre, solicitud.Direccion, solicitud.Telefono, cancelacion);

        return Created("/api/sucursales", sucursal);
    }

    /// <summary>Botón "Exportar CSV" del panel.</summary>
    [HttpGet("citas.csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarCitas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancelacion)
    {
        var csv = await _servicio.ExportarCitasCsvAsync(desde, hasta, cancelacion);

        // El BOM hace que Excel abra el archivo con los acentos correctos.
        var contenido = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

        return File(contenido, "text/csv", $"citas-{desde:yyyyMMdd}-{hasta:yyyyMMdd}.csv");
    }
}

public sealed record CambiarEstadoMedicoSolicitud(EstadoMedico Estado);

public sealed record CrearEspecialidadSolicitud(string Nombre, string? Descripcion);

public sealed record CrearSucursalSolicitud(string Nombre, string? Direccion, string? Telefono);
