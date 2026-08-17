using MediCita.Application.Abstracciones;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MediCita.Infrastructure.Persistencia.Repositorios;

public sealed class EspecialidadRepositorio : IEspecialidadRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public EspecialidadRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<Especialidad?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Especialidades.FirstOrDefaultAsync(e => e.Id == id, cancelacion);

    public async Task<IReadOnlyList<Especialidad>> ListarAsync(
        bool soloActivas = true, CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Especialidades.AsQueryable();

        if (soloActivas)
            consulta = consulta.Where(e => e.Activa);

        return await consulta.OrderBy(e => e.Nombre).ToListAsync(cancelacion);
    }

    public void Agregar(Especialidad especialidad) => _contexto.Especialidades.Add(especialidad);
}

public sealed class SucursalRepositorio : ISucursalRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public SucursalRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Sucursales.FirstOrDefaultAsync(s => s.Id == id, cancelacion);

    public async Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken cancelacion = default) =>
        await _contexto.Sucursales.Where(s => s.Activa).OrderBy(s => s.Nombre).ToListAsync(cancelacion);

    public void Agregar(Sucursal sucursal) => _contexto.Sucursales.Add(sucursal);
}

public sealed class CitaRepositorio : ICitaRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public CitaRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<Cita?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Citas.FirstOrDefaultAsync(c => c.Id == id, cancelacion);

    public Task<Cita?> ObtenerCompletaAsync(Guid id, CancellationToken cancelacion = default) =>
        ConsultaCompleta().FirstOrDefaultAsync(c => c.Id == id, cancelacion);

    public Task<Cita?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancelacion = default) =>
        ConsultaCompleta().FirstOrDefaultAsync(c => c.Codigo == codigo, cancelacion);

    public async Task<IReadOnlyList<Cita>> ObtenerDelPacienteAsync(
        Guid pacienteId, CancellationToken cancelacion = default) =>
        await ConsultaCompleta()
            .Where(c => c.PacienteId == pacienteId)
            .OrderBy(c => c.FechaHoraInicio)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Cita>> ObtenerDelMedicoEnRangoAsync(
        Guid medicoId, DateTime desde, DateTime hasta, CancellationToken cancelacion = default) =>
        await ConsultaCompleta()
            .Where(c => c.MedicoId == medicoId && c.FechaHoraInicio >= desde && c.FechaHoraInicio < hasta)
            .OrderBy(c => c.FechaHoraInicio)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Cita>> ObtenerEnRangoAsync(
        DateTime desde, DateTime hasta, CancellationToken cancelacion = default) =>
        await ConsultaCompleta()
            .Where(c => c.FechaHoraInicio >= desde && c.FechaHoraInicio < hasta)
            .OrderBy(c => c.FechaHoraInicio)
            .ToListAsync(cancelacion);

    public Task<bool> ExisteCupoOcupadoAsync(
        Guid medicoId, DateTime inicio, Guid? excluyendoCitaId = null, CancellationToken cancelacion = default) =>
        _contexto.Citas.AnyAsync(c =>
            c.MedicoId == medicoId &&
            c.FechaHoraInicio == inicio &&
            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada) &&
            (excluyendoCitaId == null || c.Id != excluyendoCitaId), cancelacion);

    public Task<bool> PacienteTieneCitaEnAsync(
        Guid pacienteId, DateTime inicio, Guid? excluyendoCitaId = null, CancellationToken cancelacion = default) =>
        _contexto.Citas.AnyAsync(c =>
            c.PacienteId == pacienteId &&
            c.FechaHoraInicio == inicio &&
            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada) &&
            (excluyendoCitaId == null || c.Id != excluyendoCitaId), cancelacion);

    /// <summary>
    /// Correlativo del año tomado de una secuencia de SQL Server, para que dos
    /// citas creadas a la vez no reciban el mismo código.
    /// </summary>
    public async Task<string> SiguienteCodigoAsync(int anio, CancellationToken cancelacion = default)
    {
        if (!_contexto.Database.IsSqlServer())
        {
            var usados = await _contexto.Citas.CountAsync(cancelacion);
            return $"{anio}-{usados + 1:D4}";
        }

        // Se ejecuta con ADO directo: SQL Server no admite NEXT VALUE FOR dentro de
        // una subconsulta, que es como lo envolvería SqlQueryRaw.
        var conexion = _contexto.Database.GetDbConnection();
        var abrir = conexion.State != System.Data.ConnectionState.Open;

        if (abrir)
            await conexion.OpenAsync(cancelacion);

        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = $"SELECT NEXT VALUE FOR dbo.{MediCitaDbContext.NombreSecuenciaCita}";

            if (_contexto.Database.CurrentTransaction is { } transaccion)
                comando.Transaction = transaccion.GetDbTransaction();

            var siguiente = Convert.ToInt32(await comando.ExecuteScalarAsync(cancelacion));
            return $"{anio}-{siguiente:D4}";
        }
        finally
        {
            if (abrir)
                await conexion.CloseAsync();
        }
    }

    public void Agregar(Cita cita) => _contexto.Citas.Add(cita);

    private IQueryable<Cita> ConsultaCompleta() =>
        _contexto.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Medico).ThenInclude(m => m!.Especialidad)
            .Include(c => c.Sucursal);
}

public sealed class NotificacionRepositorio : INotificacionRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public NotificacionRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    /// <summary>
    /// Recordatorios cuya hora ya llegó. Trae la cita con paciente y médico porque
    /// el mensaje se arma con esos datos.
    /// </summary>
    public async Task<IReadOnlyList<Notificacion>> ObtenerDespachablesAsync(
        DateTime hasta, int limite = 50, CancellationToken cancelacion = default) =>
        await _contexto.Notificaciones
            .Include(n => n.Cita).ThenInclude(c => c!.Paciente)
            .Include(n => n.Cita).ThenInclude(c => c!.Medico).ThenInclude(m => m!.Especialidad)
            .Include(n => n.Cita).ThenInclude(c => c!.Sucursal)
            .Where(n => (n.Estado == EstadoNotificacion.Pendiente || n.Estado == EstadoNotificacion.Fallida)
                        && n.FechaProgramada <= hasta)
            .OrderBy(n => n.FechaProgramada)
            .Take(limite)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Notificacion>> ObtenerDeCitaAsync(
        Guid citaId, CancellationToken cancelacion = default) =>
        await _contexto.Notificaciones
            .Where(n => n.CitaId == citaId)
            .OrderBy(n => n.FechaProgramada)
            .ToListAsync(cancelacion);

    /// <summary>
    /// Cuenta por estado. Para las enviadas el filtro de fecha aplica sobre la
    /// fecha real de envío; para el resto, sobre la fecha en que estaban programadas.
    /// </summary>
    public Task<int> ContarPorEstadoAsync(
        EstadoNotificacion estado, DateTime? desde = null, CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Notificaciones.Where(n => n.Estado == estado);

        if (desde is { } limite)
        {
            consulta = estado == EstadoNotificacion.Enviada
                ? consulta.Where(n => n.FechaEnvio >= limite)
                : consulta.Where(n => n.FechaProgramada >= limite);
        }

        return consulta.CountAsync(cancelacion);
    }

    public void Agregar(Notificacion notificacion) => _contexto.Notificaciones.Add(notificacion);
}

public sealed class BitacoraRepositorio : IBitacoraRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public BitacoraRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public async Task<IReadOnlyList<RegistroActividad>> ObtenerRecientesAsync(
        int cantidad = 10, CancellationToken cancelacion = default) =>
        await _contexto.Bitacora
            .AsNoTracking()
            .OrderByDescending(r => r.Momento)
            .Take(cantidad)
            .ToListAsync(cancelacion);

    public void Agregar(RegistroActividad registro) => _contexto.Bitacora.Add(registro);
}

public sealed class LatidoRepositorio : ILatidoRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public LatidoRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<LatidoDelWorker?> ObtenerUltimoAsync(CancellationToken cancelacion = default) =>
        _contexto.LatidosDelWorker
            .AsNoTracking()
            .OrderByDescending(l => l.Momento)
            .FirstOrDefaultAsync(cancelacion);

    public void Agregar(LatidoDelWorker latido) => _contexto.LatidosDelWorker.Add(latido);
}
