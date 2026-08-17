using MediCita.Domain.Agenda;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Comun;

namespace MediCita.Domain.Usuarios;

/// <summary>
/// Médico de la clínica. Hereda de <see cref="Usuario"/> y agrega su especialidad,
/// exequátur, consultorio y los horarios de atención que determinan los cupos que
/// el paciente ve en la pantalla de agendamiento.
/// </summary>
public sealed class Medico : Usuario
{
    private readonly List<Horario> _horarios = new();
    private readonly List<BloqueoAgenda> _bloqueos = new();

    private Medico() { }

    public Medico(
        string cedula,
        string nombre,
        string apellido,
        string correo,
        string? telefono,
        Guid especialidadId,
        string exequatur,
        Guid sucursalId,
        string? consultorio = null,
        int duracionCitaMinutos = 30)
        : base(cedula, nombre, apellido, correo, telefono, RolUsuario.Medico)
    {
        EspecialidadId = especialidadId;
        SucursalId = sucursalId;
        Exequatur = ExigirTexto(exequatur, "el exequátur");
        Consultorio = string.IsNullOrWhiteSpace(consultorio) ? null : consultorio.Trim();
        DuracionCitaMinutos = ValidarDuracion(duracionCitaMinutos);
        Estado = EstadoMedico.Activo;
    }

    public Guid EspecialidadId { get; private set; }
    public Especialidad? Especialidad { get; private set; }

    public Guid SucursalId { get; private set; }
    public Sucursal? Sucursal { get; private set; }

    public string Exequatur { get; private set; } = string.Empty;
    public string? Consultorio { get; private set; }
    public int DuracionCitaMinutos { get; private set; }
    public EstadoMedico Estado { get; private set; }

    public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();
    public IReadOnlyCollection<BloqueoAgenda> Bloqueos => _bloqueos.AsReadOnly();

    /// <summary>Solo un médico activo publica cupos y recibe citas nuevas.</summary>
    public bool RecibeCitas => Activo && Estado == EstadoMedico.Activo;

    public Horario AgregarHorario(DayOfWeek dia, TimeOnly desde, TimeOnly hasta)
    {
        var horario = new Horario(Id, dia, desde, hasta, DuracionCitaMinutos);

        if (_horarios.Any(h => h.Activo && h.SeSolapaCon(horario)))
            throw new ExcepcionDeDominio($"El médico ya tiene un horario que se solapa el {TraducirDia(dia)} entre {desde:HH\\:mm} y {hasta:HH\\:mm}.");

        _horarios.Add(horario);
        return horario;
    }

    public void QuitarHorario(Guid horarioId)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId)
            ?? throw new NoEncontradoException("el horario", horarioId);

        horario.Suspender();
    }

    public BloqueoAgenda BloquearAgenda(DateTime desde, DateTime hasta, string motivo)
    {
        var bloqueo = new BloqueoAgenda(Id, desde, hasta, motivo);
        _bloqueos.Add(bloqueo);
        return bloqueo;
    }

    public void CambiarEstado(EstadoMedico estado) => Estado = estado;

    public void ActualizarPerfil(Guid especialidadId, string exequatur, Guid sucursalId, string? consultorio, int duracionCitaMinutos)
    {
        EspecialidadId = especialidadId;
        SucursalId = sucursalId;
        Exequatur = ExigirTexto(exequatur, "el exequátur");
        Consultorio = string.IsNullOrWhiteSpace(consultorio) ? null : consultorio.Trim();
        DuracionCitaMinutos = ValidarDuracion(duracionCitaMinutos);
    }

    /// <summary>Cupos que el médico publica en una semana típica; el panel de administración lo muestra como "CUPOS/SEM".</summary>
    public int CuposSemanales => _horarios.Where(h => h.Activo).Sum(h => h.CantidadDeCupos);

    private static int ValidarDuracion(int minutos)
    {
        if (minutos is < 5 or > 240)
            throw new ExcepcionDeDominio("La duración de la cita debe estar entre 5 y 240 minutos.");

        return minutos;
    }

    private static string TraducirDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábado",
        _ => "domingo"
    };
}
