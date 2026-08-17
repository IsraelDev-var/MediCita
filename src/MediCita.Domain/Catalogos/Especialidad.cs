using MediCita.Domain.Comun;

namespace MediCita.Domain.Catalogos;

/// <summary>Especialidad médica: primer paso del agendamiento (mockup 02).</summary>
public sealed class Especialidad : EntidadBase
{
    private Especialidad() { }

    public Especialidad(string nombre, string? descripcion = null)
    {
        Nombre = string.IsNullOrWhiteSpace(nombre)
            ? throw new ExcepcionDeDominio("Debe indicar el nombre de la especialidad.")
            : nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        Activa = true;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }
    public bool Activa { get; private set; }

    public void Renombrar(string nombre, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDeDominio("Debe indicar el nombre de la especialidad.");

        Nombre = nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;
}
