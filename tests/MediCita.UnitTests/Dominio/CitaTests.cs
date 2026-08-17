using FluentAssertions;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Usuarios;
using MediCita.UnitTests.Comun;

namespace MediCita.UnitTests.Dominio;

/// <summary>Reglas de negocio de la entidad central del dominio.</summary>
public class CitaTests
{
    private readonly Sucursal _sucursal = Escenario.Sucursal();
    private readonly Especialidad _especialidad = Escenario.Especialidad();
    private readonly Medico _medico;
    private readonly Paciente _paciente = Escenario.Paciente();

    public CitaTests()
    {
        _medico = Escenario.Medico(_especialidad.Id, _sucursal.Id);
    }

    [Fact]
    public void Agendar_deja_la_cita_pendiente_y_publica_el_cambio()
    {
        var cita = Cita.Agendar(_paciente, _medico, _sucursal, Escenario.Cupo(10), "Chequeo", Escenario.Ahora);

        cita.Estado.Should().Be(EstadoCita.Pendiente);
        cita.DuracionMinutos.Should().Be(_medico.DuracionCitaMinutos);
        cita.Consultorio.Should().Be("304");
        cita.OcupaCupo.Should().BeTrue();

        cita.CambiosDeEstado.Should().ContainSingle()
            .Which.EsAgendamiento.Should().BeTrue();
    }

    [Fact]
    public void Agendar_rechaza_una_hora_que_ya_pasó()
    {
        var accion = () => Cita.Agendar(
            _paciente, _medico, _sucursal, Escenario.Ahora.AddHours(-1), null, Escenario.Ahora);

        accion.Should().Throw<ExcepcionDeDominio>()
            .WithMessage("*pasado*");
    }

    [Fact]
    public void Agendar_rechaza_un_médico_de_licencia()
    {
        _medico.CambiarEstado(EstadoMedico.DeLicencia);

        var accion = () => Cita.Agendar(_paciente, _medico, _sucursal, Escenario.Cupo(10), null, Escenario.Ahora);

        accion.Should().Throw<ExcepcionDeDominio>()
            .WithMessage("*no está recibiendo citas*");
    }

    [Fact]
    public void Agendar_rechaza_un_paciente_inactivo()
    {
        _paciente.Desactivar();

        var accion = () => Cita.Agendar(_paciente, _medico, _sucursal, Escenario.Cupo(10), null, Escenario.Ahora);

        accion.Should().Throw<ExcepcionDeDominio>()
            .WithMessage("*inactivo*");
    }

    [Fact]
    public void Confirmar_pasa_de_pendiente_a_confirmada()
    {
        var cita = CrearCita();

        cita.Confirmar();

        cita.Estado.Should().Be(EstadoCita.Confirmada);
        cita.CambiosDeEstado.Last().EstadoAnterior.Should().Be(EstadoCita.Pendiente);
    }

    [Fact]
    public void Confirmar_dos_veces_no_está_permitido()
    {
        var cita = CrearCita();
        cita.Confirmar();

        var accion = () => cita.Confirmar();

        accion.Should().Throw<ExcepcionDeDominio>()
            .WithMessage("*Confirmada*");
    }

    [Fact]
    public void Cancelar_libera_el_cupo_y_guarda_el_motivo()
    {
        var cita = CrearCita();

        cita.Cancelar("Ya no puedo asistir", Escenario.Ahora);

        cita.Estado.Should().Be(EstadoCita.Cancelada);
        cita.OcupaCupo.Should().BeFalse();
        cita.MotivoCancelacion.Should().Be("Ya no puedo asistir");
        cita.FechaCancelacion.Should().Be(Escenario.Ahora);
        cita.CambiosDeEstado.Last().DejaDeOcuparCupo.Should().BeTrue();
    }

    [Fact]
    public void Una_cita_cancelada_no_se_puede_reprogramar()
    {
        var cita = CrearCita();
        cita.Cancelar(null, Escenario.Ahora);

        var accion = () => cita.Reprogramar(Escenario.Cupo(11), _medico, Escenario.Ahora);

        accion.Should().Throw<ExcepcionDeDominio>()
            .WithMessage("*Cancelada*");
    }

    [Fact]
    public void Reprogramar_mueve_la_hora_y_deja_la_cita_pendiente_otra_vez()
    {
        var cita = CrearCita();
        cita.Confirmar();
        cita.LimpiarCambiosDeEstado();

        cita.Reprogramar(Escenario.Cupo(11), _medico, Escenario.Ahora);

        cita.FechaHoraInicio.Should().Be(Escenario.Cupo(11));
        cita.Estado.Should().Be(EstadoCita.Pendiente, "el recordatorio anterior deja de aplicar");
        cita.CambiosDeEstado.Should().ContainSingle();
    }

    [Fact]
    public void Reprogramar_al_mismo_cupo_no_tiene_sentido()
    {
        var cita = CrearCita();

        var accion = () => cita.Reprogramar(cita.FechaHoraInicio, _medico, Escenario.Ahora);

        accion.Should().Throw<ExcepcionDeDominio>()
            .WithMessage("*ya está agendada*");
    }

    [Fact]
    public void MarcarAtendida_guarda_la_nota_de_consulta()
    {
        var cita = CrearCita();
        cita.Confirmar();

        cita.MarcarAtendida("Presión 128/82.", Escenario.Ahora);

        cita.Estado.Should().Be(EstadoCita.Atendida);
        cita.NotaConsulta.Should().Be("Presión 128/82.");
        cita.FechaAtencion.Should().Be(Escenario.Ahora);
    }

    [Fact]
    public void MarcarNoAsistio_cierra_la_cita_como_ausencia()
    {
        var cita = CrearCita();
        cita.Confirmar();

        cita.MarcarNoAsistio();

        cita.Estado.Should().Be(EstadoCita.NoAsistio);
        cita.OcupaCupo.Should().BeFalse();
    }

    [Fact]
    public void El_código_solo_se_asigna_una_vez()
    {
        var cita = CrearCita();
        cita.AsignarCodigo("2026-0731");

        var accion = () => cita.AsignarCodigo("2026-0999");

        accion.Should().Throw<ExcepcionDeDominio>();
        cita.Codigo.Should().Be("2026-0731");
    }

    private Cita CrearCita() =>
        Cita.Agendar(_paciente, _medico, _sucursal, Escenario.Cupo(10), "Chequeo", Escenario.Ahora);
}
