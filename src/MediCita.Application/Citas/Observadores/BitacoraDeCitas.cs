using MediCita.Application.Abstracciones;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;

namespace MediCita.Application.Citas.Observadores;

/// <summary>
/// Segundo observador del mismo evento: escribe la línea de bitácora que el panel
/// de administración muestra en "Actividad reciente". Sirve para mostrar que se
/// pueden agregar reacciones sin tocar la entidad Cita.
/// </summary>
public sealed class BitacoraDeCitas : ICitaObservador
{
    private readonly IBitacoraRepositorio _bitacora;
    private readonly IRelojDelSistema _reloj;

    public BitacoraDeCitas(IBitacoraRepositorio bitacora, IRelojDelSistema reloj)
    {
        _bitacora = bitacora;
        _reloj = reloj;
    }

    public Task AlCambiarEstadoAsync(CambioDeEstadoCita cambio, CancellationToken cancelacion = default)
    {
        var cita = cambio.Cita;
        var paciente = cita.Paciente?.NombreCompleto ?? "un paciente";
        var codigo = string.IsNullOrWhiteSpace(cita.Codigo) ? "nueva" : cita.Codigo;

        var descripcion = cambio.EstadoNuevo switch
        {
            EstadoCita.Pendiente when cambio.EsAgendamiento => $"Cita {codigo} creada por {paciente}",
            EstadoCita.Pendiente => $"Cita {codigo} reprogramada por {paciente}",
            EstadoCita.Confirmada => $"Cita {codigo} confirmada por {paciente}",
            EstadoCita.Cancelada => $"Cita {codigo} cancelada",
            EstadoCita.Atendida => $"Cita {codigo} atendida",
            EstadoCita.NoAsistio => $"Ausencia registrada en la cita {codigo}",
            _ => $"Cita {codigo}: {cambio.Detalle}"
        };

        _bitacora.Agregar(new RegistroActividad(CategoriaActividad.Cita, descripcion, _reloj.Ahora));
        return Task.CompletedTask;
    }
}
