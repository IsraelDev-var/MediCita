using FluentAssertions;
using MediCita.Application.Servicios;
using MediCita.Domain.Citas;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;
using MediCita.UnitTests.Comun;

namespace MediCita.UnitTests.Aplicacion;

/// <summary>
/// Escenario 2 del documento: el ciclo del worker que envía los recordatorios,
/// con la tolerancia a fallas que describe la vista de procesos.
/// </summary>
public class ServicioRecordatoriosTests
{
    private readonly DatosEnMemoria _datos = new();
    private readonly RelojFijo _reloj = new(Escenario.Ahora);
    private readonly CanalDePrueba _correo = new();
    private readonly OpcionesRecordatorio _opciones = new() { IntentosMaximos = 3 };
    private readonly ServicioRecordatorios _servicio;

    private readonly Medico _medico;
    private readonly Paciente _paciente;

    public ServicioRecordatoriosTests()
    {
        var sucursal = Escenario.Sucursal();
        _medico = Escenario.Medico(Guid.NewGuid(), sucursal.Id);
        _paciente = Escenario.Paciente();

        _datos.Sucursales.Add(sucursal);
        _datos.Usuarios.Add(_medico);
        _datos.Usuarios.Add(_paciente);

        _servicio = new ServicioRecordatorios(
            new NotificacionRepositorioFalso(_datos),
            new LatidoRepositorioFalso(_datos),
            new BitacoraRepositorioFalsa(_datos),
            new SelectorDeCanalFalso(_correo, new CanalDePrueba(CanalNotificacion.Sms)),
            new GeneradorDeTokensFalso(),
            new UnidadDeTrabajoFalsa(_datos),
            _reloj,
            _opciones);
    }

    [Fact]
    public async Task Un_ciclo_sin_recordatorios_vencidos_no_envía_nada_pero_deja_latido()
    {
        ProgramarRecordatorio(Escenario.Cupo(10)); // vence 24 h antes, todavía no

        var resultado = await _servicio.EjecutarCicloAsync();

        resultado.Procesadas.Should().Be(0);
        _correo.Enviados.Should().BeEmpty();
        _datos.Latidos.Should().ContainSingle("el panel de administración usa el latido para saber que el worker vive");
    }

    [Fact]
    public async Task Envía_los_recordatorios_cuya_hora_ya_llegó_y_no_los_repite()
    {
        var recordatorio = ProgramarRecordatorio(Escenario.Cupo(10));
        _reloj.Ahora = recordatorio.FechaProgramada.AddMinutes(1);

        var primero = await _servicio.EjecutarCicloAsync();
        var segundo = await _servicio.EjecutarCicloAsync();

        primero.Enviadas.Should().Be(1);
        segundo.Procesadas.Should().Be(0, "una notificación enviada ya no se vuelve a tomar");

        _correo.Enviados.Should().ContainSingle()
            .Which.Asunto.Should().Contain("tu cita es mañana");

        recordatorio.Estado.Should().Be(EstadoNotificacion.Enviada);
    }

    [Fact]
    public async Task El_correo_lleva_los_enlaces_firmados_de_confirmar_y_reprogramar()
    {
        var recordatorio = ProgramarRecordatorio(Escenario.Cupo(10));
        _reloj.Ahora = recordatorio.FechaProgramada;

        await _servicio.EjecutarCicloAsync();

        var correo = (NotificacionCorreo)recordatorio;
        correo.UrlConfirmar.Should().Contain("confirmar");
        correo.UrlReprogramar.Should().Contain("reprogramar");
        _correo.Enviados.Single().CuerpoHtml.Should().Contain("Confirmar asistencia");
    }

    [Fact]
    public async Task Si_la_cita_se_canceló_el_recordatorio_se_anula_en_vez_de_enviarse()
    {
        var recordatorio = ProgramarRecordatorio(Escenario.Cupo(10));
        recordatorio.Cita!.Cancelar("El paciente ya no puede", Escenario.Ahora);
        _reloj.Ahora = recordatorio.FechaProgramada;

        var resultado = await _servicio.EjecutarCicloAsync();

        resultado.Enviadas.Should().Be(0);
        recordatorio.Estado.Should().Be(EstadoNotificacion.Anulada);
        _correo.Enviados.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_fallo_del_SMTP_deja_la_notificación_para_el_próximo_ciclo()
    {
        var recordatorio = ProgramarRecordatorio(Escenario.Cupo(10));
        _reloj.Ahora = recordatorio.FechaProgramada;
        _correo.Falla = true;

        var conFallo = await _servicio.EjecutarCicloAsync();

        conFallo.Fallidas.Should().Be(1);
        recordatorio.Estado.Should().Be(EstadoNotificacion.Fallida);

        _correo.Falla = false;
        var reintento = await _servicio.EjecutarCicloAsync();

        reintento.Enviadas.Should().Be(1);
        recordatorio.Estado.Should().Be(EstadoNotificacion.Enviada);
    }

    [Fact]
    public async Task Después_del_máximo_de_intentos_el_recordatorio_deja_de_reintentarse()
    {
        var recordatorio = ProgramarRecordatorio(Escenario.Cupo(10));
        _reloj.Ahora = recordatorio.FechaProgramada;
        _correo.Falla = true;

        for (var i = 0; i < _opciones.IntentosMaximos; i++)
            await _servicio.EjecutarCicloAsync();

        recordatorio.Intentos.Should().Be(_opciones.IntentosMaximos);

        await _servicio.EjecutarCicloAsync();

        recordatorio.Estado.Should().Be(EstadoNotificacion.Anulada);
        recordatorio.Intentos.Should().Be(_opciones.IntentosMaximos, "ya no se sigue intentando");
    }

    [Fact]
    public async Task Los_envíos_quedan_registrados_en_la_bitácora()
    {
        var recordatorio = ProgramarRecordatorio(Escenario.Cupo(10));
        _reloj.Ahora = recordatorio.FechaProgramada;

        await _servicio.EjecutarCicloAsync();

        _datos.Bitacora.Should().ContainSingle()
            .Which.Descripcion.Should().Be("1 recordatorio enviado");
    }

    private Notificacion ProgramarRecordatorio(DateTime inicioDeLaCita)
    {
        var cita = Cita.Agendar(_paciente, _medico, _datos.Sucursales[0], inicioDeLaCita, "Chequeo", Escenario.Ahora);
        cita.AsignarCodigo("2026-0731");
        cita.Confirmar();
        _datos.Citas.Add(cita);

        var recordatorio = NotificacionCorreo.ProgramarRecordatorio(cita, Escenario.Ahora);
        _datos.Notificaciones.Add(recordatorio);

        return recordatorio;
    }
}
