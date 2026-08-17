using MediCita.Application.Citas;
using MediCita.Application.Citas.Observadores;
using MediCita.Application.Servicios;
using MediCita.Domain.Citas;
using Microsoft.Extensions.DependencyInjection;

namespace MediCita.Application;

/// <summary>
/// Registro de la capa de aplicación. Todo se resuelve por constructor
/// (inyección de dependencias), lo que permite sustituir cualquier pieza en las
/// pruebas unitarias.
/// </summary>
public static class RegistroDeAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<ServicioAutenticacion>();
        servicios.AddScoped<ServicioCatalogos>();
        servicios.AddScoped<ServicioDisponibilidad>();
        servicios.AddScoped<ServicioCitas>();
        servicios.AddScoped<ServicioAgendaMedico>();
        servicios.AddScoped<ServicioAdministracion>();
        servicios.AddScoped<ServicioRecordatorios>();

        // Sujeto y observadores del patrón Observer: agregar una reacción nueva
        // es registrar una implementación más de ICitaObservador.
        servicios.AddScoped<IPublicadorDeCambiosDeCita, PublicadorDeCambiosDeCita>();
        servicios.AddScoped<ICitaObservador, ProgramadorDeRecordatorios>();
        servicios.AddScoped<ICitaObservador, BitacoraDeCitas>();

        return servicios;
    }
}
