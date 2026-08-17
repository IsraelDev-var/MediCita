namespace MediCita.Domain.Usuarios;

/// <summary>
/// Personal administrativo de la clínica. Gestiona médicos, especialidades y
/// horarios; nunca accede a los datos clínicos del paciente (mockup 06).
/// </summary>
public sealed class Administrador : Usuario
{
    private Administrador() { }

    public Administrador(string cedula, string nombre, string apellido, string correo, string? telefono = null)
        : base(cedula, nombre, apellido, correo, telefono, RolUsuario.Administrador)
    {
    }
}
