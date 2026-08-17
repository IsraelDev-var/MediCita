using MediCita.Domain.Comun;

namespace MediCita.Application;

/// <summary>Correo o contraseña incorrectos; la API la traduce a 401.</summary>
public sealed class CredencialesInvalidasException : ExcepcionDeDominio
{
    public CredencialesInvalidasException()
        : base("El correo o la contraseña no son correctos.") { }
}

/// <summary>El usuario está autenticado pero la cita o el recurso no le pertenece; la API la traduce a 403.</summary>
public sealed class AccesoDenegadoException : ExcepcionDeDominio
{
    public AccesoDenegadoException(string mensaje = "No tiene permiso para realizar esta acción.")
        : base(mensaje) { }
}
