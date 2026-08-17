namespace MediCita.Domain.Usuarios;

/// <summary>Roles que viajan dentro del token JWT y gobiernan el acceso a la API.</summary>
public enum RolUsuario
{
    Paciente = 1,
    Medico = 2,
    Administrador = 3
}
