using MediCita.Application.Abstracciones;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediCita.Infrastructure.Persistencia;

/// <summary>
/// Carga los datos de demostración que aparecen en los mockups: dos sedes, cinco
/// especialidades, tres médicos con sus horarios, seis pacientes y la agenda del
/// día. Es idempotente: si ya hay usuarios, no hace nada.
/// </summary>
public sealed class SembradorDeDatos
{
    public const string ContrasenaDemo = "MediCita2026";

    private readonly MediCitaDbContext _contexto;
    private readonly ICitaRepositorio _citas;
    private readonly IHasheadorDeContrasenas _hasheador;
    private readonly IRelojDelSistema _reloj;
    private readonly ILogger<SembradorDeDatos> _log;

    public SembradorDeDatos(
        MediCitaDbContext contexto,
        ICitaRepositorio citas,
        IHasheadorDeContrasenas hasheador,
        IRelojDelSistema reloj,
        ILogger<SembradorDeDatos> log)
    {
        _contexto = contexto;
        _citas = citas;
        _hasheador = hasheador;
        _reloj = reloj;
        _log = log;
    }

    public async Task SembrarAsync(CancellationToken cancelacion = default)
    {
        if (await _contexto.Usuarios.AnyAsync(cancelacion))
        {
            _log.LogInformation("La base de datos ya tiene usuarios; no se siembran datos de demostración.");
            return;
        }

        var hash = _hasheador.Hashear(ContrasenaDemo);
        var pasado = _reloj.Ahora.AddYears(-2); // referencia para saltar la validación de "no agendar en el pasado"

        // --- Sedes y especialidades ---------------------------------------------------
        var naco = new Sucursal("Sede Naco", "Av. Tiradentes 45, Naco, Santo Domingo", "809-555-0140");
        var bellaVista = new Sucursal("Sede Bella Vista", "Av. Sarasota 92, Bella Vista", "809-555-0187");
        _contexto.Sucursales.AddRange(naco, bellaVista);

        var cardiologia = new Especialidad("Cardiología", "Corazón y sistema circulatorio");
        var general = new Especialidad("Medicina general", "Consulta general y seguimiento");
        var pediatria = new Especialidad("Pediatría", "Atención de niños y adolescentes");
        var ginecologia = new Especialidad("Ginecología", "Salud de la mujer");
        var dermatologia = new Especialidad("Dermatología", "Piel, cabello y uñas");
        _contexto.Especialidades.AddRange(cardiologia, general, pediatria, ginecologia, dermatologia);

        // --- Administrador ------------------------------------------------------------
        var admin = new Administrador("001-1234567-8", "Anderson", "Calderón", "admin@medicita.do", "809-555-0100");
        admin.EstablecerContrasena(hash);
        _contexto.Usuarios.Add(admin);

        // --- Médicos y horarios -------------------------------------------------------
        var bencosme = new Medico(
            "402-1122334-5", "Laura", "Bencosme", "laura.bencosme@medicita.do", "809-555-0111",
            cardiologia.Id, "18-4402", naco.Id, "304");
        AgregarJornada(bencosme, DayOfWeek.Monday, DayOfWeek.Friday, new TimeOnly(8, 0), new TimeOnly(12, 0), new TimeOnly(14, 0), new TimeOnly(16, 0));
        bencosme.EstablecerContrasena(hash);

        var guzman = new Medico(
            "001-9988776-6", "Rafael", "Guzmán", "rafael.guzman@medicita.do", "809-555-0122",
            general.Id, "21-8890", naco.Id, "112");
        AgregarJornada(guzman, DayOfWeek.Monday, DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(13, 0), new TimeOnly(14, 0), new TimeOnly(17, 0));
        guzman.EstablecerContrasena(hash);

        var reyes = new Medico(
            "402-5566778-9", "Yuderka", "Reyes", "yuderka.reyes@medicita.do", "809-555-0133",
            cardiologia.Id, "15-2201", naco.Id, "210", 40);
        reyes.AgregarHorario(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));
        reyes.AgregarHorario(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(12, 0));
        reyes.AgregarHorario(DayOfWeek.Friday, new TimeOnly(8, 0), new TimeOnly(12, 0));
        reyes.EstablecerContrasena(hash);

        _contexto.Medicos.AddRange(bencosme, guzman, reyes);

        // --- Pacientes ----------------------------------------------------------------
        var maria = CrearPaciente("402-2345678-1", "María", "Peña", "maria.pena@correo.do", "809-555-0201", 34, "Penicilina", hash);
        var juan = CrearPaciente("001-3456789-2", "Juan Alberto", "Then", "juan.then@correo.do", "809-555-0202", 45, null, hash);
        var rosa = CrearPaciente("402-4567890-3", "Rosa Emilia", "Ureña", "rosa.urena@correo.do", "809-555-0203", 62, "Sulfas", hash);
        var elvin = CrearPaciente("001-5678901-4", "Elvin", "Rodríguez", "elvin.rodriguez@correo.do", "809-555-0204", 51, null, hash);
        var carmen = CrearPaciente("402-6789012-5", "Carmen", "Vásquez", "carmen.vasquez@correo.do", "809-555-0205", 29, null, hash);
        var pedro = CrearPaciente("001-7890123-6", "Pedro", "Santana", "pedro.santana@correo.do", "809-555-0206", 68, "Aspirina", hash);

        _contexto.Usuarios.AddRange(maria, juan, rosa, elvin, carmen, pedro);

        await _contexto.SaveChangesAsync(cancelacion);

        // --- Agenda del día del médico (mockup 05) ------------------------------------
        var dia = ProximoDiaHabil(_reloj.Hoy);

        await AgendarAsync(juan, bencosme, naco, dia.ToDateTime(new TimeOnly(8, 30)), "Seguimiento", EstadoCita.Atendida, pasado, cancelacion);
        await AgendarAsync(rosa, bencosme, naco, dia.ToDateTime(new TimeOnly(9, 0)), "Primera consulta", EstadoCita.Atendida, pasado, cancelacion);
        var citaMaria = await AgendarAsync(maria, bencosme, naco, dia.ToDateTime(new TimeOnly(10, 0)), "Chequeo de presión arterial", EstadoCita.Confirmada, pasado, cancelacion);
        await AgendarAsync(elvin, bencosme, naco, dia.ToDateTime(new TimeOnly(10, 30)), "Seguimiento", EstadoCita.Confirmada, pasado, cancelacion);
        await AgendarAsync(carmen, bencosme, naco, dia.ToDateTime(new TimeOnly(14, 0)), "Primera consulta", EstadoCita.Confirmada, pasado, cancelacion);
        await AgendarAsync(pedro, bencosme, naco, dia.ToDateTime(new TimeOnly(15, 30)), "Seguimiento", EstadoCita.Pendiente, pasado, cancelacion);

        // --- Historial de María y su próxima cita con otro médico ---------------------
        await AgendarAsync(maria, reyes, naco, ProximoDiaDe(_reloj.Hoy.AddDays(-75), DayOfWeek.Monday).ToDateTime(new TimeOnly(9, 20)), "Chequeo anual", EstadoCita.Atendida, pasado, cancelacion);
        await AgendarAsync(maria, guzman, naco, ProximoDiaDe(_reloj.Hoy.AddDays(-120), DayOfWeek.Tuesday).ToDateTime(new TimeOnly(11, 0)), "Gripe", EstadoCita.Atendida, pasado, cancelacion);
        await AgendarAsync(maria, guzman, naco, ProximoDiaDe(_reloj.Hoy.AddDays(18), DayOfWeek.Monday).ToDateTime(new TimeOnly(15, 30)), "Control general", EstadoCita.Pendiente, pasado, cancelacion);

        // La Dra. Reyes queda de licencia después de cargar su historial: por eso
        // aparece sin cupos en el panel, pero conserva sus citas ya atendidas.
        reyes.CambiarEstado(EstadoMedico.DeLicencia);
        _contexto.Bitacora.Add(new RegistroActividad(
            CategoriaActividad.Agenda, "Horario de Dra. Reyes suspendido", _reloj.Ahora.AddMinutes(-17)));

        // Recordatorio ya programado para la cita confirmada de María (mockup 03).
        _contexto.Notificaciones.Add(NotificacionCorreo.ProgramarRecordatorio(citaMaria, _reloj.Ahora));

        _contexto.Bitacora.Add(new RegistroActividad(
            CategoriaActividad.Usuario, "Datos de demostración cargados", _reloj.Ahora));

        await _contexto.SaveChangesAsync(cancelacion);

        _log.LogInformation(
            "Datos de demostración cargados. Usuarios de prueba con la contraseña '{Contrasena}'.", ContrasenaDemo);
    }

    private Paciente CrearPaciente(
        string cedula, string nombre, string apellido, string correo, string telefono,
        int edad, string? alergias, string hash)
    {
        var nacimiento = DateOnly.FromDateTime(_reloj.Ahora.AddYears(-edad).AddDays(-30));
        var paciente = new Paciente(cedula, nombre, apellido, correo, telefono, nacimiento, alergias);
        paciente.EstablecerContrasena(hash);
        return paciente;
    }

    private async Task<Cita> AgendarAsync(
        Paciente paciente, Medico medico, Sucursal sucursal, DateTime inicio,
        string motivo, EstadoCita estadoFinal, DateTime referencia, CancellationToken cancelacion)
    {
        var cita = Cita.Agendar(paciente, medico, sucursal, inicio, motivo, referencia);
        cita.AsignarCodigo(await _citas.SiguienteCodigoAsync(inicio.Year, cancelacion));

        switch (estadoFinal)
        {
            case EstadoCita.Confirmada:
                cita.Confirmar();
                break;
            case EstadoCita.Atendida:
                cita.Confirmar();
                cita.MarcarAtendida("Consulta sin novedades.", inicio.AddMinutes(30));
                break;
            case EstadoCita.NoAsistio:
                cita.Confirmar();
                cita.MarcarNoAsistio();
                break;
        }

        cita.LimpiarCambiosDeEstado();
        _contexto.Citas.Add(cita);

        return cita;
    }

    private static void AgregarJornada(
        Medico medico, DayOfWeek desde, DayOfWeek hasta,
        TimeOnly mananaInicio, TimeOnly mananaFin, TimeOnly tardeInicio, TimeOnly tardeFin)
    {
        for (var dia = desde; dia <= hasta; dia++)
        {
            medico.AgregarHorario(dia, mananaInicio, mananaFin);
            medico.AgregarHorario(dia, tardeInicio, tardeFin);
        }
    }

    /// <summary>La agenda de demostración se arma sobre el próximo día laborable.</summary>
    private static DateOnly ProximoDiaHabil(DateOnly desde)
    {
        var fecha = desde;
        while (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            fecha = fecha.AddDays(1);

        return fecha;
    }

    private static DateOnly ProximoDiaDe(DateOnly desde, DayOfWeek dia)
    {
        var fecha = desde;
        while (fecha.DayOfWeek != dia)
            fecha = fecha.AddDays(1);

        return fecha;
    }
}
