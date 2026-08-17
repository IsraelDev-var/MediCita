using MediCita.Application.Abstracciones;
using MediCita.Application.Dtos;
using MediCita.Domain.Comun;
using MediCita.Domain.Usuarios;

namespace MediCita.Application.Servicios;

/// <summary>
/// Registro y acceso de usuarios. La misma pantalla sirve a los tres roles: el
/// token devuelto lleva el rol y Angular decide a dónde redirigir.
/// </summary>
public sealed class ServicioAutenticacion
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPacienteRepositorio _pacientes;
    private readonly IBitacoraRepositorio _bitacora;
    private readonly IHasheadorDeContrasenas _hasheador;
    private readonly IGeneradorDeTokens _tokens;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IRelojDelSistema _reloj;

    public ServicioAutenticacion(
        IUsuarioRepositorio usuarios,
        IPacienteRepositorio pacientes,
        IBitacoraRepositorio bitacora,
        IHasheadorDeContrasenas hasheador,
        IGeneradorDeTokens tokens,
        IUnidadDeTrabajo unidad,
        IRelojDelSistema reloj)
    {
        _usuarios = usuarios;
        _pacientes = pacientes;
        _bitacora = bitacora;
        _hasheador = hasheador;
        _tokens = tokens;
        _unidad = unidad;
        _reloj = reloj;
    }

    public async Task<RespuestaAutenticacion> IniciarSesionAsync(SolicitudLogin solicitud, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.ObtenerPorCorreoAsync(solicitud.Correo ?? string.Empty, cancelacion);

        // Se responde igual si el correo no existe o si la contraseña no coincide,
        // para no revelar qué correos están registrados.
        if (usuario is null || !_hasheador.Verificar(solicitud.Contrasena ?? string.Empty, usuario.HashContrasena))
            throw new CredencialesInvalidasException();

        if (!usuario.Activo)
            throw new ExcepcionDeDominio("La cuenta está desactivada. Comuníquese con la clínica.");

        usuario.RegistrarAcceso();
        await _unidad.GuardarCambiosAsync(cancelacion);

        return Emitir(usuario);
    }

    public async Task<RespuestaAutenticacion> RegistrarPacienteAsync(
        SolicitudRegistroPaciente solicitud, CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Contrasena) || solicitud.Contrasena.Length < 8)
            throw new ExcepcionDeDominio("La contraseña debe tener al menos 8 caracteres.");

        var paciente = new Paciente(
            solicitud.Cedula,
            solicitud.Nombre,
            solicitud.Apellido,
            solicitud.Correo,
            solicitud.Telefono,
            solicitud.FechaNacimiento);

        if (await _usuarios.ExisteCorreoAsync(paciente.Correo, cancelacion))
            throw new ExcepcionDeDominio($"Ya existe una cuenta registrada con el correo {paciente.Correo}.");

        if (await _usuarios.ExisteCedulaAsync(paciente.Cedula, cancelacion))
            throw new ExcepcionDeDominio($"Ya existe una cuenta registrada con la cédula {paciente.Cedula}.");

        paciente.EstablecerContrasena(_hasheador.Hashear(solicitud.Contrasena));

        _pacientes.Agregar(paciente);
        _bitacora.Agregar(new RegistroActividad(
            CategoriaActividad.Usuario, $"Nuevo paciente registrado: {paciente.NombreCompleto}", _reloj.Ahora));

        await _unidad.GuardarCambiosAsync(cancelacion);

        return Emitir(paciente);
    }

    public async Task<UsuarioDto> ObtenerPerfilAsync(Guid usuarioId, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId);

        return usuario.AUsuarioDto();
    }

    public async Task ActualizarContactoAsync(
        Guid usuarioId, string correo, string? telefono, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId);

        var normalizado = correo.Trim().ToLowerInvariant();
        if (normalizado != usuario.Correo && await _usuarios.ExisteCorreoAsync(normalizado, cancelacion))
            throw new ExcepcionDeDominio($"El correo {normalizado} ya está en uso por otra cuenta.");

        usuario.ActualizarContacto(correo, telefono);
        await _unidad.GuardarCambiosAsync(cancelacion);
    }

    public async Task CambiarContrasenaAsync(
        Guid usuarioId, string contrasenaActual, string contrasenaNueva, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId);

        if (!_hasheador.Verificar(contrasenaActual ?? string.Empty, usuario.HashContrasena))
            throw new CredencialesInvalidasException();

        if (string.IsNullOrWhiteSpace(contrasenaNueva) || contrasenaNueva.Length < 8)
            throw new ExcepcionDeDominio("La contraseña nueva debe tener al menos 8 caracteres.");

        usuario.EstablecerContrasena(_hasheador.Hashear(contrasenaNueva));
        await _unidad.GuardarCambiosAsync(cancelacion);
    }

    private RespuestaAutenticacion Emitir(Usuario usuario)
    {
        var token = _tokens.Generar(usuario);
        return new RespuestaAutenticacion(token.Token, token.Expira, usuario.AUsuarioDto());
    }
}
