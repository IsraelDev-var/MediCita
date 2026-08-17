namespace MediCita.Domain.Comun;

/// <summary>Categorías con las que se agrupa la bitácora del panel de administración.</summary>
public enum CategoriaActividad
{
    Cita = 1,
    Usuario = 2,
    Agenda = 3,
    Recordatorio = 4
}

/// <summary>
/// Línea de bitácora que alimenta el bloque "Actividad reciente" del panel de
/// administración (mockup 06).
/// </summary>
public sealed class RegistroActividad : EntidadBase
{
    private RegistroActividad() { }

    public RegistroActividad(CategoriaActividad categoria, string descripcion, DateTime momento)
    {
        Categoria = categoria;
        Descripcion = string.IsNullOrWhiteSpace(descripcion)
            ? throw new ExcepcionDeDominio("La descripción de la actividad no puede quedar vacía.")
            : descripcion.Trim();
        Momento = momento;
    }

    public CategoriaActividad Categoria { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public DateTime Momento { get; private set; }
}
