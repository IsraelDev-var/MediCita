using MediCita.Domain.Comun;

namespace MediCita.Domain.Catalogos;

/// <summary>Sede de la clínica donde se presta la consulta (por ejemplo, "Sede Naco").</summary>
public sealed class Sucursal : EntidadBase
{
    private Sucursal() { }

    public Sucursal(string nombre, string? direccion = null, string? telefono = null)
    {
        Nombre = string.IsNullOrWhiteSpace(nombre)
            ? throw new ExcepcionDeDominio("Debe indicar el nombre de la sucursal.")
            : nombre.Trim();
        Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Activa = true;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public bool Activa { get; private set; }

    public void Desactivar() => Activa = false;
}
