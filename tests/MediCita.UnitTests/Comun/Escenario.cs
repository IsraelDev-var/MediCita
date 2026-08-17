using MediCita.Application.Abstracciones;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Usuarios;

namespace MediCita.UnitTests.Comun;

/// <summary>Reloj fijo para que las reglas de fecha sean verificables.</summary>
public sealed class RelojFijo : IRelojDelSistema
{
    public RelojFijo(DateTime ahora) => Ahora = ahora;

    public DateTime Ahora { get; set; }
}

/// <summary>
/// Construye el conjunto mínimo de entidades que casi todas las pruebas necesitan:
/// una sede, una especialidad, un médico con horario de mañana y tarde, y una paciente.
/// </summary>
public static class Escenario
{
    /// <summary>Lunes 13 de julio de 2026, 07:00: la misma semana que muestran los mockups.</summary>
    public static readonly DateTime Ahora = new(2026, 7, 13, 7, 0, 0);

    public static Sucursal Sucursal() => new("Sede Naco", "Av. Tiradentes 45");

    public static Especialidad Especialidad() => new("Cardiología");

    public static Medico Medico(Guid especialidadId, Guid sucursalId, int duracion = 30)
    {
        var medico = new Medico(
            "402-1122334-5", "Laura", "Bencosme", "laura.bencosme@medicita.do", "809-555-0111",
            especialidadId, "18-4402", sucursalId, "304", duracion);

        // Lunes a viernes, con el hueco de almuerzo entre las dos franjas.
        for (var dia = DayOfWeek.Monday; dia <= DayOfWeek.Friday; dia++)
        {
            medico.AgregarHorario(dia, new TimeOnly(8, 0), new TimeOnly(12, 0));
            medico.AgregarHorario(dia, new TimeOnly(14, 0), new TimeOnly(16, 0));
        }

        medico.EstablecerContrasena("hash-de-prueba");
        return medico;
    }

    public static Paciente Paciente(string cedula = "402-2345678-1", string correo = "maria.pena@correo.do")
    {
        var paciente = new Paciente(
            cedula, "María", "Peña", correo, "809-555-0201", new DateOnly(1992, 3, 14), "Penicilina");

        paciente.EstablecerContrasena("hash-de-prueba");
        return paciente;
    }

    /// <summary>Miércoles 15 de julio a la hora indicada: un cupo válido del horario de arriba.</summary>
    public static DateTime Cupo(int hora, int minutos = 0) => new(2026, 7, 15, hora, minutos, 0);
}
