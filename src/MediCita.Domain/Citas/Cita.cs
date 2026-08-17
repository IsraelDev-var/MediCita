using MediCita.Domain.Catalogos;
using MediCita.Domain.Comun;
using MediCita.Domain.Usuarios;

namespace MediCita.Domain.Citas;

/// <summary>
/// Entidad central del dominio. Concentra las reglas de agendamiento,
/// reprogramación y cancelación, y publica un evento por cada cambio de estado
/// para que los observadores (patrón Observer) generen las notificaciones.
/// </summary>
public sealed class Cita : EntidadBase
{
    private readonly List<CambioDeEstadoCita> _cambiosDeEstado = new();

    private Cita() { }

    private Cita(
        Paciente paciente,
        Medico medico,
        Sucursal sucursal,
        DateTime inicio,
        int duracionMinutos,
        string? motivoConsulta)
    {
        PacienteId = paciente.Id;
        Paciente = paciente;
        MedicoId = medico.Id;
        Medico = medico;
        SucursalId = sucursal.Id;
        Sucursal = sucursal;
        FechaHoraInicio = inicio;
        DuracionMinutos = duracionMinutos;
        MotivoConsulta = Limpiar(motivoConsulta);
        Consultorio = medico.Consultorio;
        Estado = EstadoCita.Pendiente;
    }

    /// <summary>Código legible que el paciente cita a soporte, por ejemplo "2026-0731".</summary>
    public string Codigo { get; private set; } = string.Empty;

    public Guid PacienteId { get; private set; }
    public Paciente? Paciente { get; private set; }

    public Guid MedicoId { get; private set; }
    public Medico? Medico { get; private set; }

    public Guid SucursalId { get; private set; }
    public Sucursal? Sucursal { get; private set; }

    public DateTime FechaHoraInicio { get; private set; }
    public int DuracionMinutos { get; private set; }
    public EstadoCita Estado { get; private set; }
    public string? MotivoConsulta { get; private set; }
    public string? NotaConsulta { get; private set; }
    public string? Consultorio { get; private set; }
    public string? MotivoCancelacion { get; private set; }
    public DateTime? FechaCancelacion { get; private set; }
    public DateTime? FechaAtencion { get; private set; }

    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(DuracionMinutos);

    /// <summary>Estados en los que la cita sigue ocupando el cupo del médico.</summary>
    public bool OcupaCupo => Estado is EstadoCita.Pendiente or EstadoCita.Confirmada;

    /// <summary>Cambios pendientes de publicar a los observadores.</summary>
    public IReadOnlyCollection<CambioDeEstadoCita> CambiosDeEstado => _cambiosDeEstado.AsReadOnly();

    public void LimpiarCambiosDeEstado() => _cambiosDeEstado.Clear();

    /// <summary>
    /// Crea la cita en estado Pendiente. La validación de que el cupo pertenezca
    /// al horario del médico y siga libre la hace el servicio de aplicación justo
    /// antes de llamar aquí; la base de datos la refuerza con un índice único.
    /// </summary>
    public static Cita Agendar(
        Paciente paciente,
        Medico medico,
        Sucursal sucursal,
        DateTime inicio,
        string? motivoConsulta = null,
        DateTime? ahora = null)
    {
        ArgumentNullException.ThrowIfNull(paciente);
        ArgumentNullException.ThrowIfNull(medico);
        ArgumentNullException.ThrowIfNull(sucursal);

        var referencia = ahora ?? DateTime.Now;

        if (!paciente.Activo)
            throw new ExcepcionDeDominio("El paciente está inactivo y no puede agendar citas.");

        if (!medico.RecibeCitas)
            throw new ExcepcionDeDominio($"El Dr(a). {medico.NombreCompleto} no está recibiendo citas en este momento.");

        if (inicio <= referencia)
            throw new ExcepcionDeDominio("No se puede agendar una cita en el pasado.");

        var cita = new Cita(paciente, medico, sucursal, inicio, medico.DuracionCitaMinutos, motivoConsulta);
        cita.RegistrarCambio(null, EstadoCita.Pendiente, "Cita agendada");
        return cita;
    }

    /// <summary>El código se asigna al persistir, tomando el correlativo del año.</summary>
    public void AsignarCodigo(string codigo)
    {
        if (!string.IsNullOrWhiteSpace(Codigo))
            throw new ExcepcionDeDominio("La cita ya tiene un código asignado.");

        Codigo = string.IsNullOrWhiteSpace(codigo)
            ? throw new ExcepcionDeDominio("El código de la cita no puede quedar vacío.")
            : codigo.Trim();
    }

    public void Confirmar()
    {
        ExigirEstado("confirmar", EstadoCita.Pendiente);

        var anterior = Estado;
        Estado = EstadoCita.Confirmada;
        RegistrarCambio(anterior, Estado, "Asistencia confirmada");
    }

    public void Cancelar(string? motivo, DateTime? ahora = null)
    {
        ExigirEstado("cancelar", EstadoCita.Pendiente, EstadoCita.Confirmada);

        var anterior = Estado;
        Estado = EstadoCita.Cancelada;
        MotivoCancelacion = Limpiar(motivo);
        FechaCancelacion = ahora ?? DateTime.Now;
        RegistrarCambio(anterior, Estado, MotivoCancelacion ?? "Cita cancelada");
    }

    /// <summary>
    /// Mueve la cita a un cupo nuevo. El cupo anterior solo queda libre cuando este
    /// cambio se persiste, tal como indica la nota del mockup 04.
    /// </summary>
    public void Reprogramar(DateTime nuevoInicio, Medico medico, DateTime? ahora = null)
    {
        ArgumentNullException.ThrowIfNull(medico);
        ExigirEstado("reprogramar", EstadoCita.Pendiente, EstadoCita.Confirmada);

        var referencia = ahora ?? DateTime.Now;
        if (nuevoInicio <= referencia)
            throw new ExcepcionDeDominio("No se puede reprogramar una cita hacia el pasado.");

        if (nuevoInicio == FechaHoraInicio && medico.Id == MedicoId)
            throw new ExcepcionDeDominio("La cita ya está agendada en ese horario.");

        if (!medico.RecibeCitas)
            throw new ExcepcionDeDominio($"El Dr(a). {medico.NombreCompleto} no está recibiendo citas en este momento.");

        var anterior = Estado;
        var horaAnterior = FechaHoraInicio;

        MedicoId = medico.Id;
        Medico = medico;
        Consultorio = medico.Consultorio;
        DuracionMinutos = medico.DuracionCitaMinutos;
        FechaHoraInicio = nuevoInicio;

        // Al mover la cita vuelve a quedar pendiente: el recordatorio anterior ya
        // no aplica y el observador programa uno nuevo.
        Estado = EstadoCita.Pendiente;
        RegistrarCambio(anterior, Estado, $"Reprogramada desde {horaAnterior:dd/MM/yyyy hh\\:mm tt}");
    }

    public void MarcarAtendida(string? notaConsulta = null, DateTime? ahora = null)
    {
        ExigirEstado("marcar como atendida", EstadoCita.Pendiente, EstadoCita.Confirmada);

        var anterior = Estado;
        Estado = EstadoCita.Atendida;
        NotaConsulta = Limpiar(notaConsulta) ?? NotaConsulta;
        FechaAtencion = ahora ?? DateTime.Now;
        RegistrarCambio(anterior, Estado, "Consulta atendida");
    }

    public void MarcarNoAsistio()
    {
        ExigirEstado("registrar la ausencia", EstadoCita.Pendiente, EstadoCita.Confirmada);

        var anterior = Estado;
        Estado = EstadoCita.NoAsistio;
        RegistrarCambio(anterior, Estado, "El paciente no asistió");
    }

    public void RegistrarNota(string? nota) => NotaConsulta = Limpiar(nota);

    private void ExigirEstado(string accion, params EstadoCita[] permitidos)
    {
        if (!permitidos.Contains(Estado))
            throw new ExcepcionDeDominio($"No se puede {accion} una cita en estado {Estado}.");
    }

    private void RegistrarCambio(EstadoCita? anterior, EstadoCita nuevo, string detalle) =>
        _cambiosDeEstado.Add(new CambioDeEstadoCita(this, anterior, nuevo, detalle, DateTime.UtcNow));

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
