using MediCita.Domain.Citas;
using MediCita.Domain.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCita.Infrastructure.Persistencia.Configuraciones;

public sealed class ConfiguracionCita : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> constructor)
    {
        constructor.ToTable("Citas");
        constructor.HasKey(c => c.Id);

        constructor.Property(c => c.Codigo).HasMaxLength(15).IsRequired();
        constructor.Property(c => c.Estado).HasConversion<int>();
        constructor.Property(c => c.MotivoConsulta).HasMaxLength(300);
        constructor.Property(c => c.NotaConsulta).HasMaxLength(1000);
        constructor.Property(c => c.MotivoCancelacion).HasMaxLength(300);
        constructor.Property(c => c.Consultorio).HasMaxLength(30);

        constructor.Ignore(c => c.FechaHoraFin);
        constructor.Ignore(c => c.OcupaCupo);
        constructor.Ignore(c => c.CambiosDeEstado);

        constructor.HasOne(c => c.Paciente)
            .WithMany()
            .HasForeignKey(c => c.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(c => c.Medico)
            .WithMany()
            .HasForeignKey(c => c.MedicoId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(c => c.Sucursal)
            .WithMany()
            .HasForeignKey(c => c.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasIndex(c => c.Codigo).IsUnique();
        constructor.HasIndex(c => new { c.PacienteId, c.FechaHoraInicio });
        constructor.HasIndex(c => new { c.MedicoId, c.FechaHoraInicio }, "IX_Citas_Medico_Fecha");

        // Integridad reforzada en la base de datos: dos citas vivas (Pendiente = 1,
        // Confirmada = 2) no pueden ocupar el mismo cupo del mismo médico, aunque
        // dos peticiones lleguen exactamente a la vez.
        constructor.HasIndex(c => new { c.MedicoId, c.FechaHoraInicio }, MediCitaDbContext.NombreIndiceCupo)
            .HasDatabaseName(MediCitaDbContext.NombreIndiceCupo)
            .IsUnique()
            .HasFilter("[Estado] IN (1, 2)");
    }
}

/// <summary>
/// Notificaciones en una sola tabla con discriminador por canal: correo y SMS
/// comparten estructura y solo cambia el comportamiento de Enviar().
/// </summary>
public sealed class ConfiguracionNotificacion : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> constructor)
    {
        constructor.ToTable("Notificaciones");
        constructor.HasKey(n => n.Id);

        constructor.Property(n => n.Destinatario).HasMaxLength(160).IsRequired();
        constructor.Property(n => n.Estado).HasConversion<int>();
        constructor.Property(n => n.Tipo).HasConversion<int>();
        constructor.Property(n => n.UltimoError).HasMaxLength(500);

        constructor.Ignore(n => n.Canal);
        constructor.Ignore(n => n.EstaPendiente);

        constructor.HasOne(n => n.Cita)
            .WithMany()
            .HasForeignKey(n => n.CitaId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasIndex(n => new { n.Estado, n.FechaProgramada });
        constructor.HasIndex(n => n.CitaId);

        // La columna discriminadora se llama CanalEnvio para no chocar con la
        // propiedad calculada Canal, que cada subclase resuelve por su cuenta.
        constructor.HasDiscriminator<string>("CanalEnvio")
            .HasValue<NotificacionCorreo>("Correo")
            .HasValue<NotificacionSms>("Sms");
    }
}

public sealed class ConfiguracionNotificacionCorreo : IEntityTypeConfiguration<NotificacionCorreo>
{
    public void Configure(EntityTypeBuilder<NotificacionCorreo> constructor)
    {
        // Los enlaces firmados no se guardan: son tokens de corta vida que el
        // worker vuelve a generar en cada envío.
        constructor.Ignore(n => n.UrlConfirmar);
        constructor.Ignore(n => n.UrlReprogramar);
    }
}
