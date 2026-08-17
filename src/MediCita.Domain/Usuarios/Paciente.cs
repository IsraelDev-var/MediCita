using MediCita.Domain.Comun;

namespace MediCita.Domain.Usuarios;

/// <summary>
/// Paciente de la clínica. Hereda de <see cref="Usuario"/> los datos y el
/// comportamiento de autenticación, y agrega la información clínica básica que
/// el médico consulta en su agenda (mockup 05).
/// </summary>
public sealed class Paciente : Usuario
{
    private Paciente() { }

    public Paciente(
        string cedula,
        string nombre,
        string apellido,
        string correo,
        string? telefono,
        DateOnly? fechaNacimiento = null,
        string? alergias = null)
        : base(cedula, nombre, apellido, correo, telefono, RolUsuario.Paciente)
    {
        FechaNacimiento = fechaNacimiento;
        Alergias = string.IsNullOrWhiteSpace(alergias) ? null : alergias.Trim();
    }

    public DateOnly? FechaNacimiento { get; private set; }
    public string? Alergias { get; private set; }

    /// <summary>Edad en años cumplidos; el mockup la muestra junto al nombre.</summary>
    public int? Edad
    {
        get
        {
            if (FechaNacimiento is not { } nacimiento) return null;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var edad = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-edad)) edad--;
            return edad;
        }
    }

    public void ActualizarFichaClinica(DateOnly? fechaNacimiento, string? alergias)
    {
        if (fechaNacimiento is { } fecha && fecha > DateOnly.FromDateTime(DateTime.Today))
            throw new ExcepcionDeDominio("La fecha de nacimiento no puede ser futura.");

        FechaNacimiento = fechaNacimiento;
        Alergias = string.IsNullOrWhiteSpace(alergias) ? null : alergias.Trim();
    }
}
