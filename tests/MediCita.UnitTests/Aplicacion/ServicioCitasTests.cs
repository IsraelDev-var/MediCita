using FluentAssertions;
using MediCita.Application;
using MediCita.Application.Citas;
using MediCita.Application.Citas.Observadores;
using MediCita.Application.Dtos;
using MediCita.Application.Servicios;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;
using MediCita.UnitTests.Comun;

namespace MediCita.UnitTests.Aplicacion;

/// <summary>
/// Escenario 1 del documento de arquitectura: agendar una cita, con la validación
/// del cupo y la programación del recordatorio a cargo de los observadores.
/// </summary>
public class ServicioCitasTests
{
    private readonly DatosEnMemoria _datos = new();
    private readonly RelojFijo _reloj = new(Escenario.Ahora);
    private readonly ServicioCitas _servicio;
    private readonly Medico _medico;
    private readonly Paciente _paciente;

    public ServicioCitasTests()
    {
        var sucursal = Escenario.Sucursal();
        var especialidad = Escenario.Especialidad();

        _medico = Escenario.Medico(especialidad.Id, sucursal.Id);
        _paciente = Escenario.Paciente();

        _datos.Sucursales.Add(sucursal);
        _datos.Especialidades.Add(especialidad);
        _datos.Usuarios.Add(_medico);
        _datos.Usuarios.Add(_paciente);

        var citas = new CitaRepositorioFalso(_datos);
        var medicos = new MedicoRepositorioFalso(_datos);
        var notificaciones = new NotificacionRepositorioFalso(_datos);
        var bitacora = new BitacoraRepositorioFalsa(_datos);

        var disponibilidad = new ServicioDisponibilidad(medicos, citas, _reloj);

        // Los dos observadores registrados reaccionan al mismo evento.
        var publicador = new PublicadorDeCambiosDeCita(new ICitaObservador[]
        {
            new ProgramadorDeRecordatorios(notificaciones, _reloj),
            new BitacoraDeCitas(bitacora, _reloj),
        });

        _servicio = new ServicioCitas(
            citas,
            new PacienteRepositorioFalso(_datos),
            medicos,
            new SucursalRepositorioFalso(_datos),
            notificaciones,
            disponibilidad,
            publicador,
            new UnidadDeTrabajoFalsa(_datos),
            _reloj);
    }

    [Fact]
    public async Task Agendar_crea_la_cita_con_código_y_estado_pendiente()
    {
        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), "Chequeo de presión"));

        cita.Estado.Should().Be(EstadoCita.Pendiente);
        cita.Codigo.Should().MatchRegex(@"^2026-\d{4}$");
        cita.Medico.Should().Contain("Bencosme");
        cita.Consultorio.Should().Be("304");
        _datos.Citas.Should().ContainSingle();
        _datos.VecesGuardado.Should().Be(1, "todo el caso de uso se confirma en una sola transacción");
    }

    [Fact]
    public async Task Agendar_programa_el_recordatorio_de_24_horas()
    {
        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        _datos.Notificaciones.Should().ContainSingle();

        var recordatorio = _datos.Notificaciones.Single();
        recordatorio.Should().BeOfType<NotificacionCorreo>();
        recordatorio.FechaProgramada.Should().Be(Escenario.Cupo(10).AddHours(-24));
        cita.RecordatorioProgramado.Should().Be(recordatorio.FechaProgramada);
    }

    [Fact]
    public async Task Agendar_escribe_la_bitácora_que_ve_el_administrador()
    {
        await _servicio.AgendarAsync(_paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        _datos.Bitacora.Should().ContainSingle()
            .Which.Descripcion.Should().Contain("creada por María Peña");
    }

    [Fact]
    public async Task No_se_puede_tomar_un_cupo_que_ya_está_ocupado()
    {
        var otroPaciente = Escenario.Paciente("001-3456789-2", "juan.then@correo.do");
        _datos.Usuarios.Add(otroPaciente);

        await _servicio.AgendarAsync(_paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        var accion = async () => await _servicio.AgendarAsync(
            otroPaciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        await accion.Should().ThrowAsync<CupoNoDisponibleException>();
        _datos.Citas.Should().ContainSingle();
    }

    [Fact]
    public async Task Un_cupo_cancelado_vuelve_a_estar_disponible()
    {
        var otroPaciente = Escenario.Paciente("001-3456789-2", "juan.then@correo.do");
        _datos.Usuarios.Add(otroPaciente);

        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        await _servicio.CancelarAsync(
            cita.Id, new SolicitudCancelarCita("Se me presentó un compromiso"), Actor(_paciente, RolUsuario.Paciente));

        var reasignada = await _servicio.AgendarAsync(
            otroPaciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        reasignada.Estado.Should().Be(EstadoCita.Pendiente);
        _datos.Citas.Should().HaveCount(2);
    }

    [Fact]
    public async Task No_se_puede_agendar_fuera_del_horario_publicado()
    {
        // 13:00 cae en el hueco de almuerzo, que no genera cupos.
        var accion = async () => await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(13), null));

        await accion.Should().ThrowAsync<ExcepcionDeDominio>()
            .WithMessage("*no está disponible en la agenda*");
    }

    [Fact]
    public async Task No_se_puede_agendar_en_un_cupo_bloqueado()
    {
        _medico.BloquearAgenda(Escenario.Cupo(9), Escenario.Cupo(11), "Congreso");

        var accion = async () => await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        await accion.Should().ThrowAsync<ExcepcionDeDominio>();
    }

    [Fact]
    public async Task Un_paciente_no_puede_tener_dos_citas_a_la_misma_hora()
    {
        var otroMedico = Escenario.Medico(Guid.NewGuid(), _datos.Sucursales[0].Id);
        _datos.Usuarios.Add(otroMedico);

        await _servicio.AgendarAsync(_paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        var accion = async () => await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(otroMedico.Id, Escenario.Cupo(10), null));

        await accion.Should().ThrowAsync<ExcepcionDeDominio>()
            .WithMessage("*otra cita agendada a esa misma hora*");
    }

    [Fact]
    public async Task Reprogramar_mueve_la_cita_y_reemplaza_el_recordatorio()
    {
        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        var movida = await _servicio.ReprogramarAsync(
            cita.Id, new SolicitudReprogramarCita(Escenario.Cupo(11)), Actor(_paciente, RolUsuario.Paciente));

        movida.Inicio.Should().Be(Escenario.Cupo(11));

        _datos.Notificaciones.Should().HaveCount(2);
        _datos.Notificaciones[0].Estado.Should().Be(EstadoNotificacion.Anulada, "el recordatorio anterior ya no aplica");
        _datos.Notificaciones[1].FechaProgramada.Should().Be(Escenario.Cupo(11).AddHours(-24));
    }

    [Fact]
    public async Task Cancelar_anula_el_recordatorio_pendiente()
    {
        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        await _servicio.CancelarAsync(
            cita.Id, new SolicitudCancelarCita(null), Actor(_paciente, RolUsuario.Paciente));

        _datos.Notificaciones.Single().Estado.Should().Be(EstadoNotificacion.Anulada);
    }

    [Fact]
    public async Task Un_paciente_no_puede_tocar_la_cita_de_otro()
    {
        var otroPaciente = Escenario.Paciente("001-3456789-2", "juan.then@correo.do");
        _datos.Usuarios.Add(otroPaciente);

        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        var accion = async () => await _servicio.CancelarAsync(
            cita.Id, new SolicitudCancelarCita(null), Actor(otroPaciente, RolUsuario.Paciente));

        await accion.Should().ThrowAsync<AccesoDenegadoException>();
    }

    [Fact]
    public async Task El_administrador_sí_puede_gestionar_cualquier_cita()
    {
        var cita = await _servicio.AgendarAsync(
            _paciente.Id, new SolicitudAgendarCita(_medico.Id, Escenario.Cupo(10), null));

        var cancelada = await _servicio.CancelarAsync(
            cita.Id,
            new SolicitudCancelarCita("Cerrada por la clínica"),
            new UsuarioActualFalso(Guid.NewGuid(), RolUsuario.Administrador));

        cancelada.Estado.Should().Be(EstadoCita.Cancelada);
    }

    private static UsuarioActualFalso Actor(Usuario usuario, RolUsuario rol) => new(usuario.Id, rol);
}
