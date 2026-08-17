using MediCita.Domain.Agenda;

namespace MediCita.Application.Dtos;

/// <summary>Encabezado de un día en la tira semanal (mockup 02: "8 cupos", "Cerrado").</summary>
public sealed record DiaDisponibleDto(
    DateOnly Fecha,
    string DiaCorto,
    int Dia,
    int CuposLibres,
    bool Cerrado);

public sealed record CupoDto(DateTime Inicio, DateTime Fin, EstadoCupo Estado, bool EsDeLaManana)
{
    public string Hora => Inicio.ToString("HH:mm");
}

/// <summary>Respuesta completa del paso 3: la semana y el detalle del día elegido.</summary>
public sealed record DisponibilidadDto(
    Guid MedicoId,
    string Medico,
    string Especialidad,
    DateOnly Desde,
    DateOnly Hasta,
    DateOnly FechaSeleccionada,
    IReadOnlyList<DiaDisponibleDto> Dias,
    IReadOnlyList<CupoDto> Cupos);
