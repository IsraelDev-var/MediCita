using FluentAssertions;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;
using MediCita.UnitTests.Comun;

namespace MediCita.UnitTests.Dominio;

public class HorarioTests
{
    [Fact]
    public void GenerarCupos_divide_la_franja_según_la_duración_de_la_cita()
    {
        var medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());
        var miercoles = new DateOnly(2026, 7, 15);

        var manana = medico.Horarios.First(h => h.Dia == DayOfWeek.Wednesday && h.HoraInicio.Hour == 8);
        var cupos = manana.GenerarCupos(miercoles).ToList();

        cupos.Should().HaveCount(8, "de 08:00 a 12:00 con cupos de 30 minutos");
        cupos.First().Should().Be(new DateTime(2026, 7, 15, 8, 0, 0));
        cupos.Last().Should().Be(new DateTime(2026, 7, 15, 11, 30, 0));
    }

    [Fact]
    public void GenerarCupos_no_devuelve_nada_en_un_día_sin_horario()
    {
        var medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());
        var domingo = new DateOnly(2026, 7, 19);

        var cupos = medico.Horarios.SelectMany(h => h.GenerarCupos(domingo));

        cupos.Should().BeEmpty();
    }

    [Fact]
    public void El_almuerzo_es_el_hueco_entre_franjas_y_nunca_genera_cupos()
    {
        var medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());
        var miercoles = new DateOnly(2026, 7, 15);

        var cupos = medico.Horarios
            .Where(h => h.Dia == DayOfWeek.Wednesday)
            .SelectMany(h => h.GenerarCupos(miercoles))
            .ToList();

        cupos.Should().NotContain(c => c.Hour == 12 || c.Hour == 13);
    }

    [Fact]
    public void No_se_pueden_solapar_dos_horarios_del_mismo_día()
    {
        var medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());

        var accion = () => medico.AgregarHorario(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(13, 0));

        accion.Should().Throw<ExcepcionDeDominio>().WithMessage("*solapa*");
    }

    [Fact]
    public void CuposSemanales_suma_todas_las_franjas_activas()
    {
        var medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());

        // 5 días × (8 cupos de mañana + 4 de tarde)
        medico.CuposSemanales.Should().Be(60);
    }

    [Fact]
    public void Un_bloqueo_cubre_los_cupos_que_caen_dentro_del_rango()
    {
        var medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());
        var bloqueo = medico.BloquearAgenda(Escenario.Cupo(9), Escenario.Cupo(11), "Reunión de personal");

        bloqueo.Cubre(Escenario.Cupo(9, 30), 30).Should().BeTrue();
        bloqueo.Cubre(Escenario.Cupo(8), 30).Should().BeFalse();
        bloqueo.Cubre(Escenario.Cupo(11), 30).Should().BeFalse();
    }
}

public class NotificacionTests
{
    private readonly Cita _cita;

    public NotificacionTests()
    {
        var sucursal = Escenario.Sucursal();
        var especialidad = Escenario.Especialidad();
        var medico = Escenario.Medico(especialidad.Id, sucursal.Id);
        var paciente = Escenario.Paciente();

        _cita = Cita.Agendar(paciente, medico, sucursal, Escenario.Cupo(10), "Chequeo", Escenario.Ahora);
        _cita.AsignarCodigo("2026-0731");
    }

    [Fact]
    public void El_recordatorio_queda_programado_24_horas_antes()
    {
        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, Escenario.Ahora);

        recordatorio.FechaProgramada.Should().Be(_cita.FechaHoraInicio.AddHours(-24));
        recordatorio.Estado.Should().Be(EstadoNotificacion.Pendiente);
        recordatorio.Canal.Should().Be(CanalNotificacion.Correo);
        recordatorio.Destinatario.Should().Be("maria.pena@correo.do");
    }

    [Fact]
    public void Si_la_cita_es_en_menos_de_24_horas_el_recordatorio_sale_de_inmediato()
    {
        var ahora = _cita.FechaHoraInicio.AddHours(-3);

        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, ahora);

        recordatorio.FechaProgramada.Should().Be(ahora);
        recordatorio.EsDespachable(ahora).Should().BeTrue();
    }

    [Fact]
    public async Task Enviar_marca_la_notificación_como_enviada_una_sola_vez()
    {
        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, Escenario.Ahora);
        var canal = new CanalDePrueba();

        var primero = await recordatorio.EnviarAsync(canal);
        var segundo = await recordatorio.EnviarAsync(canal);

        primero.Should().BeTrue();
        segundo.Should().BeFalse("una notificación enviada no se vuelve a enviar");
        recordatorio.Estado.Should().Be(EstadoNotificacion.Enviada);
        canal.Enviados.Should().ContainSingle();
    }

    [Fact]
    public async Task Un_fallo_del_SMTP_no_propaga_la_excepción_y_permite_reintentar()
    {
        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, Escenario.Ahora);
        var canal = new CanalDePrueba { Falla = true };

        var resultado = await recordatorio.EnviarAsync(canal);

        resultado.Should().BeFalse();
        recordatorio.Estado.Should().Be(EstadoNotificacion.Fallida);
        recordatorio.Intentos.Should().Be(1);
        recordatorio.UltimoError.Should().Contain("SMTP");
        recordatorio.EstaPendiente.Should().BeTrue("se reintenta en el próximo ciclo");

        canal.Falla = false;
        (await recordatorio.EnviarAsync(canal)).Should().BeTrue();
        recordatorio.Intentos.Should().Be(2);
    }

    [Fact]
    public void Anular_deja_sin_efecto_un_recordatorio_pendiente()
    {
        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, Escenario.Ahora);

        recordatorio.Anular();

        recordatorio.Estado.Should().Be(EstadoNotificacion.Anulada);
        recordatorio.EstaPendiente.Should().BeFalse();
    }

    [Fact]
    public void El_correo_arma_el_asunto_y_el_cuerpo_con_los_datos_de_la_cita()
    {
        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, Escenario.Ahora);

        var mensaje = recordatorio.Construir();

        mensaje.Asunto.Should().Be("Recordatorio: tu cita es mañana a las 10:00 a.m.");
        mensaje.CuerpoHtml.Should().NotBeNull();
        mensaje.CuerpoHtml!.Should().Contain("Laura Bencosme").And.Contain("Consultorio 304");
        mensaje.CuerpoTexto.Should().Contain("10:00 a.m.").And.Contain("2026-0731");
    }

    /// <summary>
    /// El mismo Enviar() heredado produce contenidos distintos según el canal:
    /// eso es el polimorfismo que describe la vista lógica.
    /// </summary>
    [Fact]
    public void El_SMS_construye_un_mensaje_corto_y_sin_HTML()
    {
        var sms = NotificacionSms.ProgramarRecordatorio(_cita, Escenario.Ahora);

        var mensaje = sms.Construir();

        mensaje.Canal.Should().Be(CanalNotificacion.Sms);
        mensaje.CuerpoHtml.Should().BeNull();
        mensaje.CuerpoTexto.Length.Should().BeLessThanOrEqualTo(160);
        mensaje.CuerpoTexto.Should().Contain("Bencosme").And.Contain("2026-0731");
    }

    [Fact]
    public async Task No_se_puede_enviar_una_notificación_por_el_canal_equivocado()
    {
        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(_cita, Escenario.Ahora);
        var canalSms = new CanalDePrueba(CanalNotificacion.Sms);

        var accion = async () => await recordatorio.EnviarAsync(canalSms);

        await accion.Should().ThrowAsync<ExcepcionDeDominio>();
    }
}

public class UsuarioTests
{
    [Fact]
    public void La_cédula_se_normaliza_al_formato_de_la_junta_central()
    {
        var paciente = Escenario.Paciente("40223456781");

        paciente.Cedula.Should().Be("402-2345678-1");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("402-2345678")]
    public void Una_cédula_con_largo_incorrecto_se_rechaza(string cedula)
    {
        var accion = () => Escenario.Paciente(cedula);

        accion.Should().Throw<ExcepcionDeDominio>().WithMessage("*11 dígitos*");
    }

    [Fact]
    public void El_correo_se_guarda_en_minúsculas()
    {
        var paciente = Escenario.Paciente(correo: "Maria.Pena@Correo.DO");

        paciente.Correo.Should().Be("maria.pena@correo.do");
    }

    [Fact]
    public void Un_correo_sin_arroba_se_rechaza()
    {
        var accion = () => Escenario.Paciente(correo: "maria.pena.correo.do");

        accion.Should().Throw<ExcepcionDeDominio>().WithMessage("*formato válido*");
    }

    [Fact]
    public void Paciente_y_Medico_heredan_el_comportamiento_de_autenticación_de_Usuario()
    {
        Usuario paciente = Escenario.Paciente();
        Usuario medico = Escenario.Medico(Guid.NewGuid(), Guid.NewGuid());

        paciente.Rol.Should().Be(RolUsuario.Paciente);
        medico.Rol.Should().Be(RolUsuario.Medico);

        foreach (var usuario in new[] { paciente, medico })
        {
            usuario.UltimoAcceso.Should().BeNull();
            usuario.RegistrarAcceso();
            usuario.UltimoAcceso.Should().NotBeNull();
            usuario.HashContrasena.Should().NotBeEmpty();
        }
    }
}
