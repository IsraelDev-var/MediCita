using MediCita.Application.Abstracciones;

namespace MediCita.Infrastructure;

/// <summary>Hora local del servidor de la clínica.</summary>
public sealed class RelojDelSistema : IRelojDelSistema
{
    public DateTime Ahora => DateTime.Now;
}
