namespace MediCita.Domain.Comun;

/// <summary>
/// Huella que deja el worker al terminar cada ciclo. La API y el worker no se
/// comunican entre sí: el panel de administración sabe que el worker está vivo
/// leyendo esta tabla, que es el único punto de contacto entre ambos procesos.
/// </summary>
public sealed class LatidoDelWorker : EntidadBase
{
    private LatidoDelWorker() { }

    public LatidoDelWorker(DateTime momento, int procesadas, int enviadas, int fallidas)
    {
        Momento = momento;
        Procesadas = procesadas;
        Enviadas = enviadas;
        Fallidas = fallidas;
    }

    public DateTime Momento { get; private set; }
    public int Procesadas { get; private set; }
    public int Enviadas { get; private set; }
    public int Fallidas { get; private set; }
}
