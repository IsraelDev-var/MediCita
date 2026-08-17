namespace MediCita.Domain.Comun;

/// <summary>
/// Raíz de todas las entidades del dominio. Aplica ocultación de la información:
/// el identificador se expone como propiedad de solo lectura hacia afuera y solo
/// la persistencia lo asigna.
/// </summary>
public abstract class EntidadBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime FechaCreacion { get; protected set; } = DateTime.UtcNow;

    public override bool Equals(object? obj) =>
        obj is EntidadBase otra && otra.GetType() == GetType() && otra.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
