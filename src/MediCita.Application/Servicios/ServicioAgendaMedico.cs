using MediCita.Application.Abstracciones;
using MediCita.Application.Citas;
using MediCita.Application.Dtos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Usuarios;

namespace MediCita.Application.Servicios;

/// <summary>
/// Agenda diaria del médico (mockup 05): la lista de pacientes del día, los
/// bloques libres que vienen del horario y el cambio de estado sin salir de la pantalla.
/// </summary>
public sealed class ServicioAgendaMedico
{
    private readonly ICitaRepositorio _citas;
    private readonly IMedicoRepositorio _medicos;
    private readonly IPublicadorDeCambiosDeCita _publicador;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IRelojDelSistema _reloj;

    public ServicioAgendaMedico(
        ICitaRepositorio citas,
        IMedicoRepositorio medicos,
        IPublicadorDeCambiosDeCita publicador,
        IUnidadDeTrabajo unidad,
        IRelojDelSistema reloj)
    {
        _citas = citas;
        _medicos = medicos;
        _publicador = publicador;
        _unidad = unidad;
        _reloj = reloj;
    }

    public async Task<AgendaDelDiaDto> ObtenerDelDiaAsync(
        Guid medicoId, DateOnly? fecha = null, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        var dia = fecha ?? Calendario.DiaPorDefecto(_reloj.Hoy);
        var inicioDia = dia.ToDateTime(TimeOnly.MinValue);

        var citasDelDia = await _citas.ObtenerDelMedicoEnRangoAsync(
            medicoId, inicioDia, inicioDia.AddDays(1), cancelacion);

        var vigentes = citasDelDia
            .Where(c => c.Estado != EstadoCita.Cancelada)
            .OrderBy(c => c.FechaHoraInicio)
            .ToList();

        var filas = new List<CitaAgendaDto>();
        foreach (var cita in vigentes)
            filas.Add(await ArmarFilaAsync(cita, cancelacion));

        var inicioMes = new DateOnly(dia.Year, dia.Month, 1);
        var citasDelMes = await _citas.ObtenerDelMedicoEnRangoAsync(
            medicoId,
            inicioMes.ToDateTime(TimeOnly.MinValue),
            inicioMes.AddMonths(1).ToDateTime(TimeOnly.MinValue),
            cancelacion);

        var ocupadas = vigentes.Select(c => c.FechaHoraInicio).ToHashSet();

        var cuposDelDia = medico.Horarios
            .Where(h => h.Activo && h.Dia == dia.DayOfWeek)
            .SelectMany(h => h.GenerarCupos(dia))
            .ToList();

        return new AgendaDelDiaDto(
            dia,
            medico.Id,
            $"Dr(a). {medico.NombreCompleto}",
            filas,
            CalcularEspacios(medico, dia).ToList(),
            vigentes.Count(c => c.Estado == EstadoCita.Atendida),
            vigentes.Count,
            cuposDelDia.Count(c => !ocupadas.Contains(c)),
            citasDelMes.Count(c => c.Estado == EstadoCita.NoAsistio));
    }

    public async Task<CitaDto> MarcarAtendidaAsync(
        Guid citaId, Guid medicoId, string? nota, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerDelMedicoAsync(citaId, medicoId, cancelacion);

        cita.MarcarAtendida(nota, _reloj.Ahora);

        await _publicador.PublicarAsync(cita, cancelacion);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return cita.ACitaDto();
    }

    public async Task<CitaDto> RegistrarAusenciaAsync(
        Guid citaId, Guid medicoId, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerDelMedicoAsync(citaId, medicoId, cancelacion);

        cita.MarcarNoAsistio();

        await _publicador.PublicarAsync(cita, cancelacion);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return cita.ACitaDto();
    }

    public async Task<CitaDto> RegistrarNotaAsync(
        Guid citaId, Guid medicoId, string? nota, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerDelMedicoAsync(citaId, medicoId, cancelacion);

        cita.RegistrarNota(nota);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return cita.ACitaDto();
    }

    private async Task<Cita> ObtenerDelMedicoAsync(Guid citaId, Guid medicoId, CancellationToken cancelacion)
    {
        var cita = await _citas.ObtenerCompletaAsync(citaId, cancelacion)
            ?? throw new NoEncontradoException("la cita", citaId);

        // El token JWT de médico limita la vista a su propia agenda.
        if (cita.MedicoId != medicoId)
            throw new AccesoDenegadoException("Esta cita no pertenece a su agenda.");

        return cita;
    }

    private async Task<CitaAgendaDto> ArmarFilaAsync(Cita cita, CancellationToken cancelacion)
    {
        var historial = await _citas.ObtenerDelPacienteAsync(cita.PacienteId, cancelacion);

        var atendidasPrevias = historial
            .Where(c => c.Id != cita.Id && c.Estado == EstadoCita.Atendida && c.FechaHoraInicio < cita.FechaHoraInicio)
            .OrderByDescending(c => c.FechaHoraInicio)
            .ToList();

        return new CitaAgendaDto(
            cita.Id,
            cita.Codigo,
            cita.FechaHoraInicio,
            cita.DuracionMinutos,
            cita.PacienteId,
            cita.Paciente?.NombreCompleto ?? "—",
            cita.Paciente?.Edad,
            cita.Paciente?.Cedula ?? "—",
            cita.Paciente?.Alergias,
            atendidasPrevias.Count == 0 ? "Primera vez" : "Seguimiento",
            cita.Estado,
            Mapeos.NombreEstado(cita.Estado),
            cita.MotivoConsulta,
            cita.NotaConsulta,
            atendidasPrevias.FirstOrDefault()?.FechaHoraInicio);
    }

    /// <summary>
    /// Huecos entre franjas de atención: el almuerzo sale de aquí, no de una
    /// configuración aparte, y por eso nunca aparece como cupo agendable.
    /// </summary>
    private static IEnumerable<EspacioAgendaDto> CalcularEspacios(Medico medico, DateOnly dia)
    {
        var franjas = medico.Horarios
            .Where(h => h.Activo && h.Dia == dia.DayOfWeek)
            .OrderBy(h => h.HoraInicio)
            .ToList();

        for (var i = 0; i < franjas.Count - 1; i++)
        {
            var fin = franjas[i].HoraFin;
            var siguiente = franjas[i + 1].HoraInicio;

            if (siguiente <= fin)
                continue;

            var etiqueta = (siguiente - fin).TotalMinutes >= 45 && fin.Hour is >= 11 and <= 14
                ? "Almuerzo — no disponible para agendar"
                : "Bloque no disponible para agendar";

            yield return new EspacioAgendaDto(dia.ToDateTime(fin), dia.ToDateTime(siguiente), etiqueta);
        }
    }
}
