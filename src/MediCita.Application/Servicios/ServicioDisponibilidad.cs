using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Domain.Agenda;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Usuarios;

namespace MediCita.Application.Servicios;

/// <summary>
/// Calcula los cupos que el paciente ve en el paso 3 del agendamiento. La
/// disponibilidad no se guarda: se deriva en vivo del horario del médico menos
/// las citas vigentes y los bloqueos de agenda.
/// </summary>
public sealed class ServicioDisponibilidad
{
    private readonly IMedicoRepositorio _medicos;
    private readonly ICitaRepositorio _citas;
    private readonly IRelojDelSistema _reloj;

    public ServicioDisponibilidad(IMedicoRepositorio medicos, ICitaRepositorio citas, IRelojDelSistema reloj)
    {
        _medicos = medicos;
        _citas = citas;
        _reloj = reloj;
    }

    /// <summary>Semana completa (lunes a sábado) con el detalle de cupos del día elegido.</summary>
    public async Task<DisponibilidadDto> ObtenerSemanaAsync(
        Guid medicoId, DateOnly? fecha = null, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        var seleccionada = fecha ?? Calendario.DiaPorDefecto(_reloj.Hoy);
        var lunes = Calendario.InicioDeSemana(seleccionada);
        var sabado = lunes.AddDays(5);

        var citas = await _citas.ObtenerDelMedicoEnRangoAsync(
            medicoId,
            lunes.ToDateTime(TimeOnly.MinValue),
            sabado.AddDays(1).ToDateTime(TimeOnly.MinValue),
            cancelacion);

        var ocupados = citas.Where(c => c.OcupaCupo).Select(c => c.FechaHoraInicio).ToHashSet();

        var dias = new List<DiaDisponibleDto>();
        var cuposDelDia = new List<CupoDto>();

        for (var dia = lunes; dia <= sabado; dia = dia.AddDays(1))
        {
            var cupos = CalcularCupos(medico, dia, ocupados).ToList();
            var abierto = medico.Horarios.Any(h => h.Activo && h.Dia == dia.DayOfWeek) && medico.RecibeCitas;

            dias.Add(new DiaDisponibleDto(
                dia,
                Mapeos.DiaCorto(dia.DayOfWeek),
                dia.Day,
                cupos.Count(c => c.Estado == EstadoCupo.Disponible),
                !abierto));

            if (dia == seleccionada)
                cuposDelDia.AddRange(cupos.Select(c => new CupoDto(c.Inicio, c.Fin, c.Estado, c.EsDeLaManana)));
        }

        return new DisponibilidadDto(
            medico.Id,
            $"Dr(a). {medico.NombreCompleto}",
            medico.Especialidad?.Nombre ?? "—",
            lunes,
            sabado,
            seleccionada,
            dias,
            cuposDelDia);
    }

    /// <summary>
    /// Verificación puntual que hace el servicio de citas antes de crear o mover
    /// una cita: el cupo debe existir en el horario y no estar bloqueado.
    /// </summary>
    public bool EsCupoValido(Medico medico, DateTime inicio)
    {
        var fecha = DateOnly.FromDateTime(inicio);

        var perteneceAlHorario = medico.Horarios
            .Where(h => h.Activo && h.Dia == fecha.DayOfWeek)
            .SelectMany(h => h.GenerarCupos(fecha))
            .Any(c => c == inicio);

        if (!perteneceAlHorario)
            return false;

        return !medico.Bloqueos.Any(b => b.Cubre(inicio, medico.DuracionCitaMinutos));
    }

    private IEnumerable<Cupo> CalcularCupos(Medico medico, DateOnly dia, IReadOnlySet<DateTime> ocupados)
    {
        if (!medico.RecibeCitas)
            yield break;

        var ahora = _reloj.Ahora;

        var horarios = medico.Horarios
            .Where(h => h.Activo && h.Dia == dia.DayOfWeek)
            .OrderBy(h => h.HoraInicio);

        foreach (var horario in horarios)
        {
            foreach (var inicio in horario.GenerarCupos(dia))
            {
                // Un cupo que ya pasó no se ofrece, aunque nadie lo haya tomado.
                if (inicio <= ahora)
                    continue;

                var bloqueado = medico.Bloqueos.Any(b => b.Cubre(inicio, horario.DuracionCupoMinutos));

                yield return bloqueado || ocupados.Contains(inicio)
                    ? Cupo.Ocupado(inicio, horario.DuracionCupoMinutos)
                    : Cupo.Disponible(inicio, horario.DuracionCupoMinutos);
            }
        }
    }

}
