using MediCita.Application.Abstracciones;
using MediCita.Application.Servicios;
using MediCita.Domain.Notificaciones;
using MediCita.Infrastructure.Notificaciones;
using MediCita.Infrastructure.Persistencia;
using MediCita.Infrastructure.Persistencia.Repositorios;
using MediCita.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MediCita.Infrastructure;

/// <summary>
/// Registro de la infraestructura: base de datos, repositorios, seguridad y
/// canales de envío. La API y el worker comparten este mismo registro.
/// </summary>
public static class RegistroDeInfraestructura
{
    public const string NombreCadenaConexion = "MediCita";

    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString(NombreCadenaConexion)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}' en la configuración.");

        servicios.AddDbContext<MediCitaDbContext>(opciones =>
            opciones.UseSqlServer(cadena, sql =>
            {
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                sql.CommandTimeout(30);
            }));

        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicios.AddScoped<IPacienteRepositorio, PacienteRepositorio>();
        servicios.AddScoped<IMedicoRepositorio, MedicoRepositorio>();
        servicios.AddScoped<IEspecialidadRepositorio, EspecialidadRepositorio>();
        servicios.AddScoped<ISucursalRepositorio, SucursalRepositorio>();
        servicios.AddScoped<ICitaRepositorio, CitaRepositorio>();
        servicios.AddScoped<INotificacionRepositorio, NotificacionRepositorio>();
        servicios.AddScoped<IBitacoraRepositorio, BitacoraRepositorio>();
        servicios.AddScoped<ILatidoRepositorio, LatidoRepositorio>();

        servicios.AddSingleton<IRelojDelSistema, RelojDelSistema>();
        servicios.AddSingleton<IHasheadorDeContrasenas, HasheadorPbkdf2>();

        servicios.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.Seccion));
        servicios.AddScoped<IGeneradorDeTokens, GeneradorDeTokensJwt>();

        servicios.Configure<OpcionesCorreo>(configuracion.GetSection(OpcionesCorreo.Seccion));
        servicios.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<OpcionesRecordatorio>>().Value);
        servicios.Configure<OpcionesRecordatorio>(configuracion.GetSection(OpcionesRecordatorio.Seccion));

        AgregarCanales(servicios, configuracion);
        servicios.AddScoped<ISelectorDeCanal, SelectorDeCanal>();

        servicios.AddScoped<SembradorDeDatos>();

        return servicios;
    }

    /// <summary>
    /// Registra las estrategias de canal disponibles. El correo se resuelve según
    /// el modo configurado; el SMS queda siempre registrado como canal alterno.
    /// </summary>
    private static void AgregarCanales(IServiceCollection servicios, IConfiguration configuracion)
    {
        var modo = configuracion.GetSection(OpcionesCorreo.Seccion)["Modo"];

        if (string.Equals(modo, nameof(ModoDeCorreo.Smtp), StringComparison.OrdinalIgnoreCase))
            servicios.AddScoped<IEstrategiaDeCanal, CanalCorreoSmtp>();
        else
            servicios.AddScoped<IEstrategiaDeCanal, CanalCorreoArchivo>();

        servicios.AddScoped<IEstrategiaDeCanal, CanalSmsSimulado>();
    }
}
