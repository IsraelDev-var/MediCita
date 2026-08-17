using FluentAssertions;
using MediCita.Application.Servicios;
using MediCita.Domain.Agenda;
using MediCita.Domain.Citas;
using MediCita.Domain.Usuarios;
using MediCita.UnitTests.Comun;

namespace MediCita.UnitTests.Aplicacion;

/// <summary>Cálculo de cupos del paso 3 del agendamiento.</summary>
public class ServicioDisponibilidadTests
{
    private readonly DatosEnMemoria _datos = new();
    private readonly RelojFijo _reloj = new(Escenario.Ahora);
    private readonly ServicioDisponibilidad _servicio;
    private readonly Medico _medico;
    private readonly Paciente _paciente;

    public ServicioDisponibilidadTests()
    {
        var sucursal = Escenario.Sucursal();
        _medico = Escenario.Medico(Guid.NewGuid(), sucursal.Id);
        _paciente = Escenario.Paciente();

        _datos.Sucursales.Add(sucursal);
        _datos.Usuarios.Add(_medico);
        _datos.Usuarios.Add(_paciente);

        _servicio = new ServicioDisponibilidad(
            new MedicoRepositorioFalso(_datos), new CitaRepositorioFalso(_datos), _reloj);
    }

    [Fact]
    public async Task La_semana_va_de_lunes_a_sábado_y_el_sábado_está_cerrado()
    {
        var disponibilidad = await _servicio.ObtenerSemanaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        disponibilidad.Desde.Should().Be(new DateOnly(2026, 7, 13));
        disponibilidad.Hasta.Should().Be(new DateOnly(2026, 7, 18));
        disponibilidad.Dias.Should().HaveCount(6);
        disponibilidad.Dias.Last().Cerrado.Should().BeTrue("el médico no tiene horario los sábados");
    }

    [Fact]
    public async Task Un_día_completo_publica_los_doce_cupos_del_horario()
    {
        var disponibilidad = await _servicio.ObtenerSemanaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        disponibilidad.Cupos.Should().HaveCount(12, "8 de mañana y 4 de tarde");
        disponibilidad.Cupos.Count(c => c.EsDeLaManana).Should().Be(8);
        disponibilidad.Cupos.Should().OnlyContain(c => c.Estado == EstadoCupo.Disponible);
    }

    [Fact]
    public async Task Una_cita_vigente_marca_su_cupo_como_ocupado()
    {
        AgregarCita(Escenario.Cupo(10), EstadoCita.Confirmada);

        var disponibilidad = await _servicio.ObtenerSemanaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        var cupo = disponibilidad.Cupos.Single(c => c.Inicio == Escenario.Cupo(10));
        cupo.Estado.Should().Be(EstadoCupo.Ocupado);

        var miercoles = disponibilidad.Dias.Single(d => d.Fecha == new DateOnly(2026, 7, 15));
        miercoles.CuposLibres.Should().Be(11);
    }

    [Fact]
    public async Task Una_cita_cancelada_no_ocupa_el_cupo()
    {
        AgregarCita(Escenario.Cupo(10), EstadoCita.Cancelada);

        var disponibilidad = await _servicio.ObtenerSemanaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        disponibilidad.Cupos.Single(c => c.Inicio == Escenario.Cupo(10))
            .Estado.Should().Be(EstadoCupo.Disponible);
    }

    [Fact]
    public async Task Los_cupos_que_ya_pasaron_no_se_ofrecen()
    {
        // El lunes 13 a las 09:15 ya no se puede tomar el cupo de las 08:00 ni el de las 09:00.
        _reloj.Ahora = new DateTime(2026, 7, 13, 9, 15, 0);

        var disponibilidad = await _servicio.ObtenerSemanaAsync(_medico.Id, new DateOnly(2026, 7, 13));

        disponibilidad.Cupos.Should().OnlyContain(c => c.Inicio > _reloj.Ahora);
        disponibilidad.Cupos.First().Inicio.Should().Be(new DateTime(2026, 7, 13, 9, 30, 0));
    }

    [Fact]
    public async Task Un_médico_de_licencia_no_publica_cupos()
    {
        _medico.CambiarEstado(EstadoMedico.DeLicencia);

        var disponibilidad = await _servicio.ObtenerSemanaAsync(_medico.Id, new DateOnly(2026, 7, 15));

        disponibilidad.Cupos.Should().BeEmpty();
        disponibilidad.Dias.Should().OnlyContain(d => d.Cerrado);
    }

    [Fact]
    public void EsCupoValido_reconoce_el_hueco_del_almuerzo()
    {
        _servicio.EsCupoValido(_medico, Escenario.Cupo(10)).Should().BeTrue();
        _servicio.EsCupoValido(_medico, Escenario.Cupo(13)).Should().BeFalse();
        _servicio.EsCupoValido(_medico, Escenario.Cupo(10, 15)).Should().BeFalse("los cupos van de 30 en 30 minutos");
    }

    private void AgregarCita(DateTime inicio, EstadoCita estado)
    {
        var cita = Cita.Agendar(_paciente, _medico, _datos.Sucursales[0], inicio, null, Escenario.Ahora);
        cita.AsignarCodigo("2026-0701");

        if (estado == EstadoCita.Confirmada) cita.Confirmar();
        if (estado == EstadoCita.Cancelada) cita.Cancelar(null, Escenario.Ahora);

        _datos.Citas.Add(cita);
    }
}
