using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Domain.Comun;

namespace MediCita.Application.Servicios;

/// <summary>Datos de los pasos 1 y 2 del agendamiento: especialidades, sucursales y médicos.</summary>
public sealed class ServicioCatalogos
{
    private readonly IEspecialidadRepositorio _especialidades;
    private readonly ISucursalRepositorio _sucursales;
    private readonly IMedicoRepositorio _medicos;

    public ServicioCatalogos(
        IEspecialidadRepositorio especialidades,
        ISucursalRepositorio sucursales,
        IMedicoRepositorio medicos)
    {
        _especialidades = especialidades;
        _sucursales = sucursales;
        _medicos = medicos;
    }

    public async Task<IReadOnlyList<EspecialidadDto>> ListarEspecialidadesAsync(CancellationToken cancelacion = default)
    {
        var especialidades = await _especialidades.ListarAsync(soloActivas: true, cancelacion);
        var medicos = await _medicos.ListarAsync(cancelacion: cancelacion);

        return especialidades
            .Select(e => e.AEspecialidadDto(medicos.Count(m => m.EspecialidadId == e.Id)))
            .OrderBy(e => e.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<SucursalDto>> ListarSucursalesAsync(CancellationToken cancelacion = default)
    {
        var sucursales = await _sucursales.ListarAsync(cancelacion);
        return sucursales.Select(s => s.ASucursalDto()).ToList();
    }

    public async Task<IReadOnlyList<MedicoDto>> ListarMedicosAsync(
        Guid? especialidadId = null,
        Guid? sucursalId = null,
        bool soloActivos = true,
        CancellationToken cancelacion = default)
    {
        var medicos = await _medicos.ListarAsync(especialidadId, sucursalId, soloActivos, cancelacion);
        return medicos.Select(m => m.AMedicoDto()).ToList();
    }

    public async Task<MedicoDto> ObtenerMedicoAsync(Guid medicoId, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        return medico.AMedicoDto();
    }

    public async Task<IReadOnlyList<HorarioDto>> ObtenerHorariosAsync(Guid medicoId, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        return medico.Horarios
            .OrderBy(h => h.Dia == DayOfWeek.Sunday ? 7 : (int)h.Dia)
            .ThenBy(h => h.HoraInicio)
            .Select(h => h.AHorarioDto())
            .ToList();
    }
}
