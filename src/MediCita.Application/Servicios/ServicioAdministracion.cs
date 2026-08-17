using System.Globalization;
using System.Text;
using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;

namespace MediCita.Application.Servicios;

/// <summary>
/// Panel de administración (mockup 06): indicadores de la semana, estado de los
/// procesos y alta de médicos, especialidades y horarios. Nunca expone datos
/// clínicos del paciente.
/// </summary>
public sealed class ServicioAdministracion
{
    private readonly ICitaRepositorio _citas;
    private readonly IMedicoRepositorio _medicos;
    private readonly IPacienteRepositorio _pacientes;
    private readonly IEspecialidadRepositorio _especialidades;
    private readonly ISucursalRepositorio _sucursales;
    private readonly INotificacionRepositorio _notificaciones;
    private readonly IBitacoraRepositorio _bitacora;
    private readonly ILatidoRepositorio _latidos;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IHasheadorDeContrasenas _hasheador;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IRelojDelSistema _reloj;

    public ServicioAdministracion(
        ICitaRepositorio citas,
        IMedicoRepositorio medicos,
        IPacienteRepositorio pacientes,
        IEspecialidadRepositorio especialidades,
        ISucursalRepositorio sucursales,
        INotificacionRepositorio notificaciones,
        IBitacoraRepositorio bitacora,
        ILatidoRepositorio latidos,
        IUsuarioRepositorio usuarios,
        IHasheadorDeContrasenas hasheador,
        IUnidadDeTrabajo unidad,
        IRelojDelSistema reloj)
    {
        _citas = citas;
        _medicos = medicos;
        _pacientes = pacientes;
        _especialidades = especialidades;
        _sucursales = sucursales;
        _notificaciones = notificaciones;
        _bitacora = bitacora;
        _latidos = latidos;
        _usuarios = usuarios;
        _hasheador = hasheador;
        _unidad = unidad;
        _reloj = reloj;
    }

    public async Task<ResumenOperativoDto> ObtenerResumenAsync(
        DateOnly? semanaDe = null, CancellationToken cancelacion = default)
    {
        var referencia = semanaDe ?? Calendario.DiaPorDefecto(_reloj.Hoy);
        var lunes = Calendario.InicioDeSemana(referencia);
        var domingo = lunes.AddDays(6);

        var citasSemana = await CitasDeLaSemanaAsync(lunes, cancelacion);
        var citasPrevias = await CitasDeLaSemanaAsync(lunes.AddDays(-7), cancelacion);

        var vigentes = citasSemana.Where(c => c.Estado != EstadoCita.Cancelada).ToList();
        var vigentesPrevias = citasPrevias.Where(c => c.Estado != EstadoCita.Cancelada).ToList();

        var medicos = await _medicos.ListarAsync(soloActivos: false, cancelacion: cancelacion);
        var activos = medicos.Where(m => m.RecibeCitas).ToList();
        var cuposPublicados = activos.Sum(m => m.CuposSemanales);

        var recordatoriosEnviados = await _notificaciones.ContarPorEstadoAsync(
            EstadoNotificacion.Enviada, lunes.ToDateTime(TimeOnly.MinValue), cancelacion);

        var enCola = await _notificaciones.ContarPorEstadoAsync(EstadoNotificacion.Fallida, null, cancelacion);
        var pendientes = await _notificaciones.ContarPorEstadoAsync(EstadoNotificacion.Pendiente, null, cancelacion);

        var actividad = await _bitacora.ObtenerRecientesAsync(6, cancelacion);
        var latido = await _latidos.ObtenerUltimoAsync(cancelacion);

        var citasPorDia = new List<IndicadorDiarioDto>();
        for (var dia = lunes; dia <= lunes.AddDays(5); dia = dia.AddDays(1))
        {
            citasPorDia.Add(new IndicadorDiarioDto(
                dia,
                Mapeos.DiaCorto(dia.DayOfWeek),
                vigentes.Count(c => DateOnly.FromDateTime(c.FechaHoraInicio) == dia)));
        }

        var medicosOperativos = activos
            .Select(m => new MedicoOperativoDto(
                m.Id,
                $"Dr(a). {m.NombreCompleto}",
                m.Especialidad?.Nombre ?? "—",
                m.CuposSemanales,
                Porcentaje(vigentes.Count(c => c.MedicoId == m.Id), m.CuposSemanales),
                m.Estado,
                Mapeos.NombreEstado(m.Estado)))
            .Concat(medicos.Where(m => !m.RecibeCitas).Select(m => new MedicoOperativoDto(
                m.Id,
                $"Dr(a). {m.NombreCompleto}",
                m.Especialidad?.Nombre ?? "—",
                m.CuposSemanales,
                0,
                m.Estado,
                Mapeos.NombreEstado(m.Estado))))
            .ToList();

        return new ResumenOperativoDto(
            lunes,
            domingo,
            vigentes.Count,
            Variacion(vigentes.Count, vigentesPrevias.Count),
            Ausentismo(vigentes),
            Math.Round(Ausentismo(vigentes) - Ausentismo(vigentesPrevias), 1),
            Porcentaje(vigentes.Count, cuposPublicados),
            cuposPublicados,
            recordatoriosEnviados,
            enCola + pendientes,
            citasPorDia,
            medicosOperativos,
            new EstadoSistemaDto("Operativa", latido?.Momento, enCola + pendientes, true),
            actividad.Select(a => a.AActividadDto()).ToList());
    }

    public async Task<MedicoDto> CrearMedicoAsync(SolicitudNuevoMedico solicitud, CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Contrasena) || solicitud.Contrasena.Length < 8)
            throw new ExcepcionDeDominio("La contraseña debe tener al menos 8 caracteres.");

        var especialidad = await _especialidades.ObtenerPorIdAsync(solicitud.EspecialidadId, cancelacion)
            ?? throw new NoEncontradoException("la especialidad", solicitud.EspecialidadId);

        var sucursal = await _sucursales.ObtenerPorIdAsync(solicitud.SucursalId, cancelacion)
            ?? throw new NoEncontradoException("la sucursal", solicitud.SucursalId);

        var medico = new Medico(
            solicitud.Cedula,
            solicitud.Nombre,
            solicitud.Apellido,
            solicitud.Correo,
            solicitud.Telefono,
            especialidad.Id,
            solicitud.Exequatur,
            sucursal.Id,
            solicitud.Consultorio,
            solicitud.DuracionCitaMinutos);

        if (await _usuarios.ExisteCorreoAsync(medico.Correo, cancelacion))
            throw new ExcepcionDeDominio($"Ya existe una cuenta con el correo {medico.Correo}.");

        if (await _usuarios.ExisteCedulaAsync(medico.Cedula, cancelacion))
            throw new ExcepcionDeDominio($"Ya existe una cuenta con la cédula {medico.Cedula}.");

        medico.EstablecerContrasena(_hasheador.Hashear(solicitud.Contrasena));

        _medicos.Agregar(medico);
        _bitacora.Agregar(new RegistroActividad(
            CategoriaActividad.Usuario, $"Nuevo médico registrado: Dr(a). {medico.NombreCompleto}", _reloj.Ahora));

        await _unidad.GuardarCambiosAsync(cancelacion);

        return (await _medicos.ObtenerConAgendaAsync(medico.Id, cancelacion))!.AMedicoDto();
    }

    public async Task<MedicoDto> CambiarEstadoMedicoAsync(
        Guid medicoId, EstadoMedico estado, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        medico.CambiarEstado(estado);

        _bitacora.Agregar(new RegistroActividad(
            CategoriaActividad.Agenda,
            $"Dr(a). {medico.NombreCompleto} pasó a estado {Mapeos.NombreEstado(estado).ToLower(Mapeos.Cultura)}",
            _reloj.Ahora));

        await _unidad.GuardarCambiosAsync(cancelacion);
        return medico.AMedicoDto();
    }

    public async Task<HorarioDto> AgregarHorarioAsync(
        Guid medicoId, SolicitudNuevoHorario solicitud, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        var horario = medico.AgregarHorario(solicitud.Dia, solicitud.HoraInicio, solicitud.HoraFin);

        _bitacora.Agregar(new RegistroActividad(
            CategoriaActividad.Agenda,
            $"Horario de Dr(a). {medico.NombreCompleto} actualizado ({Mapeos.DiaLargo(solicitud.Dia)})",
            _reloj.Ahora));

        await _unidad.GuardarCambiosAsync(cancelacion);
        return horario.AHorarioDto();
    }

    public async Task SuspenderHorarioAsync(Guid medicoId, Guid horarioId, CancellationToken cancelacion = default)
    {
        var medico = await _medicos.ObtenerConAgendaAsync(medicoId, cancelacion)
            ?? throw new NoEncontradoException("el médico", medicoId);

        medico.QuitarHorario(horarioId);

        _bitacora.Agregar(new RegistroActividad(
            CategoriaActividad.Agenda, $"Horario de Dr(a). {medico.NombreCompleto} suspendido", _reloj.Ahora));

        await _unidad.GuardarCambiosAsync(cancelacion);
    }

    public async Task<EspecialidadDto> CrearEspecialidadAsync(
        string nombre, string? descripcion, CancellationToken cancelacion = default)
    {
        var especialidad = new Especialidad(nombre, descripcion);
        _especialidades.Agregar(especialidad);

        await _unidad.GuardarCambiosAsync(cancelacion);
        return especialidad.AEspecialidadDto();
    }

    public async Task<SucursalDto> CrearSucursalAsync(
        string nombre, string? direccion, string? telefono, CancellationToken cancelacion = default)
    {
        var sucursal = new Sucursal(nombre, direccion, telefono);
        _sucursales.Agregar(sucursal);

        await _unidad.GuardarCambiosAsync(cancelacion);
        return sucursal.ASucursalDto();
    }

    public async Task<IReadOnlyList<PacienteDto>> ListarPacientesAsync(
        string? busqueda = null, CancellationToken cancelacion = default)
    {
        var pacientes = await _pacientes.ListarAsync(busqueda, cancelacion);
        return pacientes.Select(p => p.APacienteDto()).ToList();
    }

    /// <summary>Exportación del botón "Exportar CSV" del panel.</summary>
    public async Task<string> ExportarCitasCsvAsync(
        DateOnly desde, DateOnly hasta, CancellationToken cancelacion = default)
    {
        var citas = await _citas.ObtenerEnRangoAsync(
            desde.ToDateTime(TimeOnly.MinValue),
            hasta.AddDays(1).ToDateTime(TimeOnly.MinValue),
            cancelacion);

        var csv = new StringBuilder();
        csv.AppendLine("Codigo;Fecha;Hora;Paciente;Medico;Especialidad;Sucursal;Estado");

        foreach (var cita in citas.OrderBy(c => c.FechaHoraInicio))
        {
            csv.AppendLine(string.Join(';',
                Escapar(cita.Codigo),
                cita.FechaHoraInicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cita.FechaHoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture),
                Escapar(cita.Paciente?.NombreCompleto),
                Escapar(cita.Medico?.NombreCompleto),
                Escapar(cita.Medico?.Especialidad?.Nombre),
                Escapar(cita.Sucursal?.Nombre),
                Escapar(Mapeos.NombreEstado(cita.Estado))));
        }

        return csv.ToString();
    }

    private async Task<IReadOnlyList<Cita>> CitasDeLaSemanaAsync(DateOnly lunes, CancellationToken cancelacion) =>
        await _citas.ObtenerEnRangoAsync(
            lunes.ToDateTime(TimeOnly.MinValue),
            lunes.AddDays(7).ToDateTime(TimeOnly.MinValue),
            cancelacion);

    /// <summary>Ausentismo: porcentaje de citas cerradas en las que el paciente no se presentó.</summary>
    private static double Ausentismo(IReadOnlyCollection<Cita> citas)
    {
        var cerradas = citas.Count(c => c.Estado is EstadoCita.Atendida or EstadoCita.NoAsistio);
        if (cerradas == 0) return 0;

        return Math.Round(citas.Count(c => c.Estado == EstadoCita.NoAsistio) * 100d / cerradas, 1);
    }

    private static double Porcentaje(int parte, int total) =>
        total == 0 ? 0 : Math.Round(parte * 100d / total, 1);

    private static double Variacion(int actual, int anterior) =>
        anterior == 0 ? (actual == 0 ? 0 : 100) : Math.Round((actual - anterior) * 100d / anterior, 1);

    private static string Escapar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : valor.Replace(';', ',').Replace('\n', ' ').Trim();
}
