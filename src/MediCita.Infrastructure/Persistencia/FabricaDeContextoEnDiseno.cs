using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediCita.Infrastructure.Persistencia;

/// <summary>
/// Permite ejecutar "dotnet ef migrations" contra este proyecto sin levantar la
/// API. La cadena puede sobreescribirse con la variable de entorno
/// MEDICITA_CONEXION.
/// </summary>
public sealed class FabricaDeContextoEnDiseno : IDesignTimeDbContextFactory<MediCitaDbContext>
{
    private const string CadenaPorDefecto =
        "Server=(localdb)\\MSSQLLocalDB;Database=MediCita;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public MediCitaDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("MEDICITA_CONEXION") ?? CadenaPorDefecto;

        var opciones = new DbContextOptionsBuilder<MediCitaDbContext>()
            .UseSqlServer(cadena)
            .Options;

        return new MediCitaDbContext(opciones);
    }
}
