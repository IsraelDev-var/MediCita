using FluentAssertions;
using MediCita.Application;
using MediCita.Application.Citas;
using MediCita.Application.Citas.Observadores;
using MediCita.Application.Servicios;
using MediCita.Domain.Citas;
using MediCita.Domain.Usuarios;
using MediCita.UnitTests.Comun;

namespace MediCita.UnitTests.Aplicacion;

/// <summary>Agenda diaria del médico (mockup 05).</summary>
public class ServicioAgendaMedicoTests
{
    private readonly DatosEnMemoria _datos = new();
    private readonly RelojFijo _reloj = new(Escenario.Ahora);
    private readonly ServicioAgendaMedico _servicio;
    private readonly Medico _medico;
    private readonly Paciente _paciente;

    public ServicioAgendaMedicoTests()
    {
        var sucursal = Escenario.Sucursal();
        _medico = Escenario.Medico(Guid.NewGuid(), sucursal.Id);
        _paciente = Escenario.Paciente();

        _datos.Sucursales.Add(sucursal);
        _datos.Usuarios.Add(_medico);
        _datos.Usuarios.Add(_paciente);

        var citas = new CitaRepositorioFalso(_datos);

        var publicador = new PublicadorDeCambiosDeCita(new ICitaObservador[]
        {
            new ProgramadorDeRecordatorios(new NotificacionRepositorioFalso(_datos), _reloj),
            new BitacoraDeCitas(new BitacoraRepositorioFalsa(_datos), _reloj),
        });

        _servicio = new ServicioAgendaMedico(
            citas, new MedicoRepositorioFalso(_datos), publicador, new UnidadDeTrabajoFalsa(_datos), _reloj);
    }

    [Fact]
    public async Task La_agenda_del_día_lista_las_citas_y_el_bloque_de_almuerzo()
    {
        AgregarCita(Escenario.Cupo(8, 30), EstadoCita.Atendida);
        AgregarCita(Escenario.Cupo(10), EstadoCita.Confirmada);
        AgregarCita(Escenario.Cupo(14), EstadoCita.Pendiente);

        var agenda = await _servicio.ObtenerDelDiaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        agenda.Citas.Should().HaveCount(3);
        agenda.TotalDelDia.Should().Be(3);
        agenda.AtendidasHoy.Should().Be(1);
        agenda.CuposLibres.Should().Be(9, "de 12 cupos hay 3 tomados");

        agenda.Espacios.Should().ContainSingle()
            .Which.Etiqueta.Should().Contain("Almuerzo");
    }

    [Fact]
    public async Task Una_cita_cancelada_no_aparece_en_la_agenda_del_día()
    {
        AgregarCita(Escenario.Cupo(10), EstadoCita.Cancelada);

        var agenda = await _servicio.ObtenerDelDiaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        agenda.Citas.Should().BeEmpty();
    }

    [Fact]
    public async Task El_primer_paciente_aparece_como_primera_vez_y_luego_como_seguimiento()
    {
        AgregarCita(new DateTime(2026, 6, 10, 10, 0, 0), EstadoCita.Atendida);
        AgregarCita(Escenario.Cupo(10), EstadoCita.Confirmada);

        var agenda = await _servicio.ObtenerDelDiaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        var fila = agenda.Citas.Single();
        fila.TipoVisita.Should().Be("Seguimiento");
        fila.UltimaVisita.Should().Be(new DateTime(2026, 6, 10, 10, 0, 0));
        fila.Alergias.Should().Be("Penicilina");
        fila.EdadPaciente.Should().NotBeNull();
    }

    [Fact]
    public async Task Marcar_como_atendida_guarda_la_nota_de_consulta()
    {
        var cita = AgregarCita(Escenario.Cupo(10), EstadoCita.Confirmada);

        var actualizada = await _servicio.MarcarAtendidaAsync(cita.Id, _medico.Id, "Presión 128/82.");

        actualizada.Estado.Should().Be(EstadoCita.Atendida);
        actualizada.NotaConsulta.Should().Be("Presión 128/82.");
        _datos.Bitacora.Should().Contain(r => r.Descripcion.Contains("atendida"));
    }

    [Fact]
    public async Task Registrar_ausencia_cierra_la_cita_como_no_asistió()
    {
        var cita = AgregarCita(Escenario.Cupo(10), EstadoCita.Confirmada);

        var actualizada = await _servicio.RegistrarAusenciaAsync(cita.Id, _medico.Id);

        actualizada.Estado.Should().Be(EstadoCita.NoAsistio);
    }

    [Fact]
    public async Task Un_médico_no_puede_tocar_la_cita_de_otro()
    {
        var cita = AgregarCita(Escenario.Cupo(10), EstadoCita.Confirmada);
        var otroMedico = Escenario.Medico(Guid.NewGuid(), _datos.Sucursales[0].Id);
        _datos.Usuarios.Add(otroMedico);

        var accion = async () => await _servicio.MarcarAtendidaAsync(cita.Id, otroMedico.Id, null);

        await accion.Should().ThrowAsync<AccesoDenegadoException>();
    }

    private Cita AgregarCita(DateTime inicio, EstadoCita estado)
    {
        // La referencia se toma del día anterior a la cita para poder sembrar historial.
        var cita = Cita.Agendar(_paciente, _medico, _datos.Sucursales[0], inicio, "Chequeo", inicio.AddDays(-1));
        cita.AsignarCodigo($"2026-{_datos.Citas.Count + 700:D4}");

        if (estado is EstadoCita.Confirmada or EstadoCita.Atendida) cita.Confirmar();
        if (estado == EstadoCita.Atendida) cita.MarcarAtendida("Consulta previa.", inicio.AddMinutes(30));
        if (estado == EstadoCita.Cancelada) cita.Cancelar(null, Escenario.Ahora);

        cita.LimpiarCambiosDeEstado();
        _datos.Citas.Add(cita);

        return cita;
    }
}
