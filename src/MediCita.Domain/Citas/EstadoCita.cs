namespace MediCita.Domain.Citas;

/// <summary>
/// Ciclo de vida de una cita. Los cinco valores son los que la interfaz muestra
/// como etiquetas en "Mis citas" y en la agenda del médico.
/// </summary>
public enum EstadoCita
{
    Pendiente = 1,
    Confirmada = 2,
    Atendida = 3,
    Cancelada = 4,
    NoAsistio = 5
}
