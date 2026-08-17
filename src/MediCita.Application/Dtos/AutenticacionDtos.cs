using MediCita.Domain.Usuarios;

namespace MediCita.Application.Dtos;

/// <summary>Credenciales de la pantalla 01.</summary>
public sealed record SolicitudLogin(string Correo, string Contrasena);

/// <summary>Alta de paciente con cédula, correo y teléfono (pestaña "Crear cuenta").</summary>
public sealed record SolicitudRegistroPaciente(
    string Cedula,
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    string Contrasena,
    DateOnly? FechaNacimiento = null);

public sealed record UsuarioDto(
    Guid Id,
    string Cedula,
    string Nombre,
    string Apellido,
    string NombreCompleto,
    string Correo,
    string? Telefono,
    RolUsuario Rol,
    string RolNombre);

/// <summary>El token y el usuario con el que Angular decide a qué pantalla redirigir.</summary>
public sealed record RespuestaAutenticacion(string Token, DateTime Expira, UsuarioDto Usuario);
