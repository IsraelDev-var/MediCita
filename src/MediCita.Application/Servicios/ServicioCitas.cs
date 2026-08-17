using MediCita.Application.Abstracciones;
using MediCita.Application.Citas;
using MediCita.Application.Dtos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Usuarios;

namespace MediCita.Application.Servicios;

/// <summary>
/// Caso de uso central del sistema (escenario 1 del documento). Verifica el cupo
/// contra el horario del médico, crea la cita en estado Pendiente y deja que los
/// observadores programen el recordatorio, todo dentro de una misma transacción.
/// </summary>
public sealed class ServicioCitas
{
    private readonly ICitaRepositorio _citas;
    private readonly IPacienteRepositorio _pacientes;
    private readonly IMedicoRepositorio _medicos;
    private readonly ISucursalRepositorio _sucursales;
    private readonly INotificacionRepositorio _notificaciones;
    private readonly ServicioDisponibilidad _disponibilidad;
    private readonly IPublicadorDeCambiosDeCita _publicador;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IRelojDelSistema _reloj;

    public ServicioCitas(
        ICitaRepositorio citas,
        IPacienteRepositorio pacientes,
        IMedicoRepositorio medicos,
        ISucursalRepositorio sucursales,
        INotificacionRepositorio notificaciones,
        ServicioDisponibilidad disponibilidad,
        IPublicadorDeCambiosDeCita publicador,
        IUnidadDeTrabajo unidad,
        IRelojDelSistema reloj)
    {
        _citas = citas;
        _pacientes = pacientes;
        _medicos = medicos;
        _sucursales = sucursales;
        _notificaciones = notificaciones;
        _disponibilidad = disponibilidad;
        _publicador = publicador;
        _unidad = unidad;
        _reloj = reloj;
    }

    public async Task<CitaDto> AgendarAsync(
        Guid pacienteId, SolicitudAgendarCita solicitud, CancellationToken cancelacion = default)
    {
        var paciente = await _pacientes.ObtenerPorIdAsync(pacienteId, cancelacion)
            ?? throw new NoEncontradoException("el paciente", pacienteId);

        var medico = await _medicos.ObtenerConAgendaAsync(solicitud.MedicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", solicitud.MedicoId);

        var sucursal = await _sucursales.ObtenerPorIdAsync(medico.SucursalId, cancelacion)
            ?? throw new NoEncontradoException("la sucursal", medico.SucursalId);

        await ValidarCupoAsync(medico, solicitud.Inicio, paciente.Id, null, cancelacion);

        var cita = Cita.Agendar(paciente, medico, sucursal, solicitud.Inicio, solicitud.MotivoConsulta, _reloj.Ahora);
        cita.AsignarCodigo(await _citas.SiguienteCodigoAsync(solicitud.Inicio.Year, cancelacion));

        _citas.Agregar(cita);

        // Los observadores programan el recordatorio y escriben la bitácora.
        await _publicador.PublicarAsync(cita, cancelacion);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return await ArmarDtoAsync(cita, cancelacion);
    }

    public async Task<CitaDto> ReprogramarAsync(
        Guid citaId, SolicitudReprogramarCita solicitud, IUsuarioActual actor, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerConAccesoAsync(citaId, actor, cancelacion);

        var medicoId = solicitud.MedicoId ?? cita.MedicoId;
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        await ValidarCupoAsync(medico, solicitud.NuevoInicio, cita.PacienteId, cita.Id, cancelacion);

        cita.Reprogramar(solicitud.NuevoInicio, medico, _reloj.Ahora);

        await _publicador.PublicarAsync(cita, cancelacion);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return await ArmarDtoAsync(cita, cancelacion);
    }

    public async Task<CitaDto> ConfirmarAsync(Guid citaId, IUsuarioActual actor, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerConAccesoAsync(citaId, actor, cancelacion);

        cita.Confirmar();

        await _publicador.PublicarAsync(cita, cancelacion);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return await ArmarDtoAsync(cita, cancelacion);
    }

    public async Task<CitaDto> CancelarAsync(
        Guid citaId, SolicitudCancelarCita solicitud, IUsuarioActual actor, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerConAccesoAsync(citaId, actor, cancelacion);

        cita.Cancelar(solicitud.Motivo, _reloj.Ahora);

        await _publicador.PublicarAsync(cita, cancelacion);
        await _unidad.GuardarCambiosAsync(cancelacion);

        return await ArmarDtoAsync(cita, cancelacion);
    }

    public async Task<IReadOnlyList<CitaDto>> ObtenerDelPacienteAsync(
        Guid pacienteId, CancellationToken cancelacion = default)
    {
        var citas = await _citas.ObtenerDelPacienteAsync(pacienteId, cancelacion);

        return citas
            .OrderBy(c => c.FechaHoraInicio)
            .Select(c => c.ACitaDto())
            .ToList();
    }

    public async Task<CitaDto> ObtenerAsync(Guid citaId, IUsuarioActual actor, CancellationToken cancelacion = default)
    {
        var cita = await ObtenerConAccesoAsync(citaId, actor, cancelacion);
        return await ArmarDtoAsync(cita, cancelacion);
    }

    /// <summary>
    /// Integridad del cupo: debe pertenecer al horario publicado, seguir libre y no
    /// chocar con otra cita del mismo paciente. La base de datos lo refuerza con un
    /// índice único, por si dos peticiones llegan a la vez.
    /// </summary>
    private async Task ValidarCupoAsync(
        Medico medico, DateTime inicio, Guid pacienteId, Guid? citaExcluida, CancellationToken cancelacion)
    {
        if (!_disponibilidad.EsCupoValido(medico, inicio))
            throw new ExcepcionDeDominio("El horario seleccionado no está disponible en la agenda del médico.");

        if (await _citas.ExisteCupoOcupadoAsync(medico.Id, inicio, citaExcluida, cancelacion))
            throw new CupoNoDisponibleException(inicio);

        if (await _citas.PacienteTieneCitaEnAsync(pacienteId, inicio, citaExcluida, cancelacion))
            throw new ExcepcionDeDominio("Ya tienes otra cita agendada a esa misma hora.");
    }

    private async Task<Cita> ObtenerConAccesoAsync(Guid citaId, IUsuarioActual actor, CancellationToken cancelacion)
    {
        var cita = await _citas.ObtenerCompletaAsync(citaId, cancelacion)
            ?? throw new NoEncontradoException("la cita", citaId);

        var permitido = actor.Rol switch
        {
            RolUsuario.Administrador => true,
            RolUsuario.Medico => actor.Id == cita.MedicoId,
            RolUsuario.Paciente => actor.Id == cita.PacienteId,
            _ => false
        };

        if (!permitido)
            throw new AccesoDenegadoException("Esta cita pertenece a otro usuario.");

        return cita;
    }

    private async Task<CitaDto> ArmarDtoAsync(Cita cita, CancellationToken cancelacion)
    {
        var notificaciones = await _notificaciones.ObtenerDeCitaAsync(cita.Id, cancelacion);

        var recordatorio = notificaciones
            .Where(n => n.Estado != Domain.Notificaciones.EstadoNotificacion.Anulada)
            .OrderByDescending(n => n.FechaProgramada)
            .FirstOrDefault();

        return cita.ACitaDto(recordatorio);
    }
}
