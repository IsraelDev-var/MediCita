using MediCita.Application.Abstracciones;
using MediCita.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace MediCita.Infrastructure.Persistencia.Repositorios;

public sealed class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public UsuarioRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancelacion);

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancelacion = default)
    {
        var normalizado = (correo ?? string.Empty).Trim().ToLower();
        return _contexto.Usuarios.FirstOrDefaultAsync(u => u.Correo == normalizado, cancelacion);
    }

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion = default)
    {
        var normalizado = (correo ?? string.Empty).Trim().ToLower();
        return _contexto.Usuarios.AnyAsync(u => u.Correo == normalizado, cancelacion);
    }

    public Task<bool> ExisteCedulaAsync(string cedula, CancellationToken cancelacion = default) =>
        _contexto.Usuarios.AnyAsync(u => u.Cedula == cedula, cancelacion);
}

public sealed class PacienteRepositorio : IPacienteRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public PacienteRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<Paciente?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Pacientes.FirstOrDefaultAsync(p => p.Id == id, cancelacion);

    public async Task<IReadOnlyList<Paciente>> ListarAsync(
        string? busqueda = null, CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Pacientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(p =>
                EF.Functions.Like(p.Nombre, $"%{texto}%") ||
                EF.Functions.Like(p.Apellido, $"%{texto}%") ||
                EF.Functions.Like(p.Cedula, $"%{texto}%") ||
                EF.Functions.Like(p.Correo, $"%{texto}%"));
        }

        return await consulta
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Apellido)
            .ToListAsync(cancelacion);
    }

    public Task<int> ContarAsync(CancellationToken cancelacion = default) =>
        _contexto.Pacientes.CountAsync(cancelacion);

    public void Agregar(Paciente paciente) => _contexto.Pacientes.Add(paciente);
}

public sealed class MedicoRepositorio : IMedicoRepositorio
{
    private readonly MediCitaDbContext _contexto;

    public MedicoRepositorio(MediCitaDbContext contexto) => _contexto = contexto;

    public Task<Medico?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Medicos.FirstOrDefaultAsync(m => m.Id == id, cancelacion);

    /// <summary>Incluye horarios y bloqueos porque de ellos sale el cálculo de cupos.</summary>
    public Task<Medico?> ObtenerConAgendaAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Medicos
            .Include(m => m.Especialidad)
            .Include(m => m.Sucursal)
            .Include(m => m.Horarios)
            .Include(m => m.Bloqueos)
            .FirstOrDefaultAsync(m => m.Id == id, cancelacion);

    public async Task<IReadOnlyList<Medico>> ListarAsync(
        Guid? especialidadId = null,
        Guid? sucursalId = null,
        bool soloActivos = true,
        CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Medicos
            .Include(m => m.Especialidad)
            .Include(m => m.Sucursal)
            .Include(m => m.Horarios)
            .AsQueryable();

        if (especialidadId is { } especialidad)
            consulta = consulta.Where(m => m.EspecialidadId == especialidad);

        if (sucursalId is { } sucursal)
            consulta = consulta.Where(m => m.SucursalId == sucursal);

        if (soloActivos)
            consulta = consulta.Where(m => m.Activo && m.Estado == EstadoMedico.Activo);

        return await consulta
            .OrderBy(m => m.Nombre)
            .ThenBy(m => m.Apellido)
            .ToListAsync(cancelacion);
    }

    public void Agregar(Medico medico) => _contexto.Medicos.Add(medico);
}
