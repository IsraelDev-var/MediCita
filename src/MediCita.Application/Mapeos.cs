using System.Globalization;
using MediCita.Application.Dtos;
using MediCita.Domain.Agenda;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;

namespace MediCita.Application;

/// <summary>
/// Traducción de entidades a DTOs. Se hace a mano y en un solo lugar para que el
/// contrato REST no quede atado a la forma interna del dominio.
/// </summary>
public static class Mapeos
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private static readonly string[] DiasCortos = ["DOM", "LUN", "MAR", "MIÉ", "JUE", "VIE", "SÁB"];

    private static readonly string[] DiasLargos =
        ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    private static readonly string[] DiasAbreviados = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];

    public static string DiaCorto(DayOfWeek dia) => DiasCortos[(int)dia];

    public static string DiaLargo(DayOfWeek dia) => DiasLargos[(int)dia];

    public static string NombreEstado(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => "Pendiente",
        EstadoCita.Confirmada => "Confirmada",
        EstadoCita.Atendida => "Atendida",
        EstadoCita.Cancelada => "Cancelada",
        EstadoCita.NoAsistio => "No asistió",
        _ => estado.ToString()
    };

    public static string NombreEstado(EstadoMedico estado) => estado switch
    {
        EstadoMedico.Activo => "Activo",
        EstadoMedico.DeLicencia => "De licencia",
        EstadoMedico.Inactivo => "Inactivo",
        _ => estado.ToString()
    };

    public static string NombreRol(RolUsuario rol) => rol switch
    {
        RolUsuario.Paciente => "Paciente",
        RolUsuario.Medico => "Médico",
        RolUsuario.Administrador => "Administrador",
        _ => rol.ToString()
    };

    public static UsuarioDto AUsuarioDto(this Usuario usuario) => new(
        usuario.Id,
        usuario.Cedula,
        usuario.Nombre,
        usuario.Apellido,
        usuario.NombreCompleto,
        usuario.Correo,
        usuario.Telefono,
        usuario.Rol,
        NombreRol(usuario.Rol));

    public static EspecialidadDto AEspecialidadDto(this Especialidad especialidad, int cantidadMedicos = 0) =>
        new(especialidad.Id, especialidad.Nombre, especialidad.Descripcion, cantidadMedicos);

    public static SucursalDto ASucursalDto(this Sucursal sucursal) =>
        new(sucursal.Id, sucursal.Nombre, sucursal.Direccion, sucursal.Telefono);

    public static MedicoDto AMedicoDto(this Medico medico) => new(
        medico.Id,
        $"Dr(a). {medico.NombreCompleto}",
        medico.EspecialidadId,
        medico.Especialidad?.Nombre ?? "—",
        medico.Exequatur,
        medico.SucursalId,
        medico.Sucursal?.Nombre ?? "—",
        medico.Consultorio,
        medico.DuracionCitaMinutos,
        medico.Estado,
        NombreEstado(medico.Estado),
        ResumenDeHorario(medico.Horarios),
        medico.CuposSemanales);

    public static HorarioDto AHorarioDto(this Horario horario) => new(
        horario.Id,
        horario.Dia,
        DiaLargo(horario.Dia),
        horario.HoraInicio,
        horario.HoraFin,
        horario.DuracionCupoMinutos,
        horario.CantidadDeCupos,
        horario.Activo);

    /// <summary>Convierte los días trabajados en un texto corto: "Lun a Jue", "Lun, Mié, Vie".</summary>
    public static string ResumenDeHorario(IEnumerable<Horario> horarios)
    {
        var dias = horarios.Where(h => h.Activo)
            .Select(h => (int)h.Dia)
            .Distinct()
            .OrderBy(d => d == 0 ? 7 : d) // el domingo va al final, como en el calendario de la clínica
            .ToList();

        if (dias.Count == 0) return "Sin horario publicado";
        if (dias.Count == 1) return DiasAbreviados[dias[0]];

        var consecutivos = dias.Zip(dias.Skip(1), (a, b) => b - a).All(diferencia => diferencia == 1);

        return consecutivos
            ? $"{DiasAbreviados[dias[0]]} a {DiasAbreviados[dias[^1]]}"
            : string.Join(", ", dias.Select(d => DiasAbreviados[d]));
    }

    public static CitaDto ACitaDto(this Cita cita, Notificacion? recordatorio = null) => new(
        cita.Id,
        cita.Codigo,
        cita.FechaHoraInicio,
        cita.FechaHoraFin,
        cita.DuracionMinutos,
        cita.Estado,
        NombreEstado(cita.Estado),
        cita.MedicoId,
        cita.Medico is null ? "—" : $"Dr(a). {cita.Medico.NombreCompleto}",
        cita.Medico?.Especialidad?.Nombre ?? "—",
        cita.Sucursal?.Nombre ?? "—",
        cita.Consultorio,
        cita.PacienteId,
        cita.Paciente?.NombreCompleto ?? "—",
        cita.Paciente?.Correo ?? string.Empty,
        cita.MotivoConsulta,
        cita.NotaConsulta,
        recordatorio?.FechaProgramada,
        recordatorio?.Estado);

    public static ActividadDto AActividadDto(this RegistroActividad registro) =>
        new(registro.Momento, registro.Categoria, registro.Descripcion);

    public static PacienteDto APacienteDto(this Paciente paciente, int citasTotales = 0) => new(
        paciente.Id,
        paciente.Cedula,
        paciente.NombreCompleto,
        paciente.Correo,
        paciente.Telefono,
        paciente.Edad,
        paciente.Alergias,
        paciente.Activo,
        citasTotales);
}
