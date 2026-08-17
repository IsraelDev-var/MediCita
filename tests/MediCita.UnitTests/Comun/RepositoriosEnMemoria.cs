using MediCita.Application.Abstracciones;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;

namespace MediCita.UnitTests.Comun;

/// <summary>
/// Implementaciones en memoria de los repositorios. Al depender la aplicación solo
/// de las interfaces (patrón Repository), las pruebas corren sin base de datos.
/// </summary>
public sealed class DatosEnMemoria
{
    public List<Usuario> Usuarios { get; } = new();
    public List<Especialidad> Especialidades { get; } = new();
    public List<Sucursal> Sucursales { get; } = new();
    public List<Cita> Citas { get; } = new();
    public List<Notificacion> Notificaciones { get; } = new();
    public List<RegistroActividad> Bitacora { get; } = new();
    public List<LatidoDelWorker> Latidos { get; } = new();

    public int VecesGuardado { get; set; }
    public int SiguienteCodigo { get; set; } = 700;
}

public sealed class UnidadDeTrabajoFalsa : IUnidadDeTrabajo
{
    private readonly DatosEnMemoria _datos;

    public UnidadDeTrabajoFalsa(DatosEnMemoria datos) => _datos = datos;

    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        _datos.VecesGuardado++;
        return Task.FromResult(1);
    }
}

public sealed class PacienteRepositorioFalso : IPacienteRepositorio
{
    private readonly DatosEnMemoria _datos;

    public PacienteRepositorioFalso(DatosEnMemoria datos) => _datos = datos;

    public Task<Paciente?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Usuarios.OfType<Paciente>().FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Paciente>> ListarAsync(string? busqueda = null, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Paciente>>(_datos.Usuarios.OfType<Paciente>().ToList());

    public Task<int> ContarAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Usuarios.OfType<Paciente>().Count());

    public void Agregar(Paciente paciente) => _datos.Usuarios.Add(paciente);
}

public sealed class MedicoRepositorioFalso : IMedicoRepositorio
{
    private readonly DatosEnMemoria _datos;

    public MedicoRepositorioFalso(DatosEnMemoria datos) => _datos = datos;

    public Task<Medico?> ObtenerConAgendaAsync(Guid id, CancellationToken cancelacion = default) =>
        ObtenerPorIdAsync(id, cancelacion);

    public Task<Medico?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Usuarios.OfType<Medico>().FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Medico>> ListarAsync(
        Guid? especialidadId = null, Guid? sucursalId = null, bool soloActivos = true,
        CancellationToken cancelacion = default)
    {
        var medicos = _datos.Usuarios.OfType<Medico>()
            .Where(m => especialidadId is null || m.EspecialidadId == especialidadId)
            .Where(m => sucursalId is null || m.SucursalId == sucursalId)
            .Where(m => !soloActivos || m.RecibeCitas)
            .ToList();

        return Task.FromResult<IReadOnlyList<Medico>>(medicos);
    }

    public void Agregar(Medico medico) => _datos.Usuarios.Add(medico);
}

public sealed class SucursalRepositorioFalso : ISucursalRepositorio
{
    private readonly DatosEnMemoria _datos;

    public SucursalRepositorioFalso(DatosEnMemoria datos) => _datos = datos;

    public Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Sucursales.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Sucursal>>(_datos.Sucursales);

    public void Agregar(Sucursal sucursal) => _datos.Sucursales.Add(sucursal);
}

public sealed class CitaRepositorioFalso : ICitaRepositorio
{
    private readonly DatosEnMemoria _datos;

    public CitaRepositorioFalso(DatosEnMemoria datos) => _datos = datos;

    public Task<Cita?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Citas.FirstOrDefault(c => c.Id == id));

    public Task<Cita?> ObtenerCompletaAsync(Guid id, CancellationToken cancelacion = default) =>
        ObtenerPorIdAsync(id, cancelacion);

    public Task<Cita?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Citas.FirstOrDefault(c => c.Codigo == codigo));

    public Task<IReadOnlyList<Cita>> ObtenerDelPacienteAsync(Guid pacienteId, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Cita>>(_datos.Citas.Where(c => c.PacienteId == pacienteId).ToList());

    public Task<IReadOnlyList<Cita>> ObtenerDelMedicoEnRangoAsync(
        Guid medicoId, DateTime desde, DateTime hasta, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Cita>>(_datos.Citas
            .Where(c => c.MedicoId == medicoId && c.FechaHoraInicio >= desde && c.FechaHoraInicio < hasta)
            .ToList());

    public Task<IReadOnlyList<Cita>> ObtenerEnRangoAsync(
        DateTime desde, DateTime hasta, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Cita>>(_datos.Citas
            .Where(c => c.FechaHoraInicio >= desde && c.FechaHoraInicio < hasta)
            .ToList());

    public Task<bool> ExisteCupoOcupadoAsync(
        Guid medicoId, DateTime inicio, Guid? excluyendoCitaId = null, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Citas.Any(c =>
            c.MedicoId == medicoId && c.FechaHoraInicio == inicio && c.OcupaCupo && c.Id != excluyendoCitaId));

    public Task<bool> PacienteTieneCitaEnAsync(
        Guid pacienteId, DateTime inicio, Guid? excluyendoCitaId = null, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Citas.Any(c =>
            c.PacienteId == pacienteId && c.FechaHoraInicio == inicio && c.OcupaCupo && c.Id != excluyendoCitaId));

    public Task<string> SiguienteCodigoAsync(int anio, CancellationToken cancelacion = default) =>
        Task.FromResult($"{anio}-{++_datos.SiguienteCodigo:D4}");

    public void Agregar(Cita cita) => _datos.Citas.Add(cita);
}

public sealed class NotificacionRepositorioFalso : INotificacionRepositorio
{
    private readonly DatosEnMemoria _datos;

    public NotificacionRepositorioFalso(DatosEnMemoria datos) => _datos = datos;

    public Task<IReadOnlyList<Notificacion>> ObtenerDespachablesAsync(
        DateTime hasta, int limite = 50, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Notificacion>>(_datos.Notificaciones
            .Where(n => n.EstaPendiente && n.FechaProgramada <= hasta)
            .OrderBy(n => n.FechaProgramada)
            .Take(limite)
            .ToList());

    public Task<IReadOnlyList<Notificacion>> ObtenerDeCitaAsync(Guid citaId, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Notificacion>>(_datos.Notificaciones.Where(n => n.CitaId == citaId).ToList());

    public Task<int> ContarPorEstadoAsync(
        EstadoNotificacion estado, DateTime? desde = null, CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Notificaciones.Count(n => n.Estado == estado));

    public void Agregar(Notificacion notificacion) => _datos.Notificaciones.Add(notificacion);
}

public sealed class BitacoraRepositorioFalsa : IBitacoraRepositorio
{
    private readonly DatosEnMemoria _datos;

    public BitacoraRepositorioFalsa(DatosEnMemoria datos) => _datos = datos;

    public Task<IReadOnlyList<RegistroActividad>> ObtenerRecientesAsync(
        int cantidad = 10, CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<RegistroActividad>>(
            _datos.Bitacora.OrderByDescending(r => r.Momento).Take(cantidad).ToList());

    public void Agregar(RegistroActividad registro) => _datos.Bitacora.Add(registro);
}

public sealed class LatidoRepositorioFalso : ILatidoRepositorio
{
    private readonly DatosEnMemoria _datos;

    public LatidoRepositorioFalso(DatosEnMemoria datos) => _datos = datos;

    public Task<LatidoDelWorker?> ObtenerUltimoAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(_datos.Latidos.OrderByDescending(l => l.Momento).FirstOrDefault());

    public void Agregar(LatidoDelWorker latido) => _datos.Latidos.Add(latido);
}

/// <summary>Estrategia de canal que registra los envíos y puede fingir una caída del SMTP.</summary>
public sealed class CanalDePrueba : IEstrategiaDeCanal
{
    public CanalDePrueba(CanalNotificacion canal = CanalNotificacion.Correo) => Canal = canal;

    public CanalNotificacion Canal { get; }

    public bool Falla { get; set; }

    public List<MensajeNotificacion> Enviados { get; } = new();

    public Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancelacion = default)
    {
        if (Falla)
            throw new InvalidOperationException("El servicio SMTP no responde.");

        Enviados.Add(mensaje);
        return Task.CompletedTask;
    }
}

public sealed class SelectorDeCanalFalso : ISelectorDeCanal
{
    private readonly Dictionary<CanalNotificacion, IEstrategiaDeCanal> _canales;

    public SelectorDeCanalFalso(params IEstrategiaDeCanal[] canales) =>
        _canales = canales.ToDictionary(c => c.Canal);

    public bool EstaDisponible(CanalNotificacion canal) => _canales.ContainsKey(canal);

    public IEstrategiaDeCanal Para(CanalNotificacion canal) => _canales[canal];
}

/// <summary>Generador de tokens de prueba: no firma nada, solo devuelve texto predecible.</summary>
public sealed class GeneradorDeTokensFalso : IGeneradorDeTokens
{
    public TokenEmitido Generar(Usuario usuario) =>
        new($"token-de-{usuario.Correo}", DateTime.UtcNow.AddHours(8));

    public string GenerarEnlaceDeAccion(Usuario usuario, string accion, Guid citaId, TimeSpan vigencia) =>
        $"enlace-{accion}-{citaId}";
}

/// <summary>Identidad del usuario que hace la petición en las pruebas.</summary>
public sealed class UsuarioActualFalso : IUsuarioActual
{
    public UsuarioActualFalso(Guid? id, RolUsuario? rol)
    {
        Id = id;
        Rol = rol;
    }

    public Guid? Id { get; }
    public RolUsuario? Rol { get; }
}
