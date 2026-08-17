using MediCita.Application.Servicios;
using Microsoft.Extensions.Options;

namespace MediCita.Worker;

/// <summary>
/// Proceso independiente de la API (vista de procesos, Figura 3). Despierta cada
/// pocos minutos, envía los recordatorios cuya hora llegó y vuelve a dormir. No
/// se comunica con la API: ambos hablan solo a través de la base de datos, así que
/// si este proceso se cae, el agendamiento sigue funcionando.
/// </summary>
public sealed class TareaDeRecordatorios : BackgroundService
{
    private readonly IServiceScopeFactory _alcances;
    private readonly OpcionesRecordatorio _opciones;
    private readonly ILogger<TareaDeRecordatorios> _log;

    public TareaDeRecordatorios(
        IServiceScopeFactory alcances,
        IOptions<OpcionesRecordatorio> opciones,
        ILogger<TareaDeRecordatorios> log)
    {
        _alcances = alcances;
        _opciones = opciones.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        var intervalo = TimeSpan.FromMinutes(Math.Max(1, _opciones.MinutosEntreCiclos));

        _log.LogInformation(
            "Worker de recordatorios iniciado. Ciclo cada {Minutos} minuto(s).", intervalo.TotalMinutes);

        using var temporizador = new PeriodicTimer(intervalo);

        do
        {
            await EjecutarCicloAsync(cancelacion);
        }
        while (await EsperarAsync(temporizador, cancelacion));

        _log.LogInformation("Worker de recordatorios detenido.");
    }

    private async Task EjecutarCicloAsync(CancellationToken cancelacion)
    {
        try
        {
            using var alcance = _alcances.CreateScope();
            var servicio = alcance.ServiceProvider.GetRequiredService<ServicioRecordatorios>();

            var resultado = await servicio.EjecutarCicloAsync(cancelacion);

            if (resultado.Procesadas > 0)
            {
                _log.LogInformation(
                    "Ciclo {Momento:HH:mm}: {Procesadas} procesadas, {Enviadas} enviadas, {Fallidas} fallidas.",
                    resultado.Momento, resultado.Procesadas, resultado.Enviadas, resultado.Fallidas);
            }
            else
            {
                _log.LogDebug("Ciclo {Momento:HH:mm}: no había recordatorios por enviar.", resultado.Momento);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Un ciclo fallido no tumba el worker: se reintenta en el siguiente.
            _log.LogError(ex, "El ciclo de recordatorios falló; se reintentará en el próximo intervalo.");
        }
    }

    private static async Task<bool> EsperarAsync(PeriodicTimer temporizador, CancellationToken cancelacion)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(cancelacion);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
