using MediCita.Application.Abstracciones;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MediCita.Infrastructure.Persistencia;

/// <summary>
/// Confirma la transacción y traduce la violación del índice único de cupo en la
/// excepción de dominio correspondiente: así la carrera entre dos pacientes que
/// eligen el mismo horario termina en un 409 claro y no en un error de base de datos.
/// </summary>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private const int ErrorClaveDuplicada = 2627;
    private const int ErrorIndiceUnicoDuplicado = 2601;

    private readonly MediCitaDbContext _contexto;

    public UnidadDeTrabajo(MediCitaDbContext contexto) => _contexto = contexto;

    public async Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        try
        {
            return await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (EsChoqueDeCupo(ex))
        {
            var inicio = _contexto.ChangeTracker.Entries<Cita>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => e.Entity.FechaHoraInicio)
                .FirstOrDefault();

            throw new CupoNoDisponibleException(inicio == default ? DateTime.Now : inicio);
        }
    }

    private static bool EsChoqueDeCupo(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && sql.Number is ErrorClaveDuplicada or ErrorIndiceUnicoDuplicado
        && sql.Message.Contains(MediCitaDbContext.NombreIndiceCupo, StringComparison.OrdinalIgnoreCase);
}
