using MediCita.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCita.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Jerarquía de usuarios en una sola tabla (TPH). La columna Rol hace de
/// discriminador, de modo que la herencia del dominio no obliga a hacer joins.
/// </summary>
public sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> constructor)
    {
        constructor.ToTable("Usuarios");
        constructor.HasKey(u => u.Id);

        constructor.Property(u => u.Cedula).HasMaxLength(15).IsRequired();
        constructor.Property(u => u.Nombre).HasMaxLength(80).IsRequired();
        constructor.Property(u => u.Apellido).HasMaxLength(80).IsRequired();
        constructor.Property(u => u.Correo).HasMaxLength(160).IsRequired();
        constructor.Property(u => u.Telefono).HasMaxLength(25);
        constructor.Property(u => u.HashContrasena).HasMaxLength(400).IsRequired();
        constructor.Property(u => u.Rol).HasConversion<int>();

        constructor.Ignore(u => u.NombreCompleto);

        constructor.HasIndex(u => u.Correo).IsUnique();
        constructor.HasIndex(u => u.Cedula).IsUnique();

        constructor.HasDiscriminator(u => u.Rol)
            .HasValue<Paciente>(RolUsuario.Paciente)
            .HasValue<Medico>(RolUsuario.Medico)
            .HasValue<Administrador>(RolUsuario.Administrador);
    }
}

public sealed class ConfiguracionPaciente : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> constructor)
    {
        constructor.Property(p => p.Alergias).HasMaxLength(300);
        constructor.Ignore(p => p.Edad);
    }
}

public sealed class ConfiguracionMedico : IEntityTypeConfiguration<Medico>
{
    public void Configure(EntityTypeBuilder<Medico> constructor)
    {
        constructor.Property(m => m.Exequatur).HasMaxLength(30);
        constructor.Property(m => m.Consultorio).HasMaxLength(30);
        constructor.Property(m => m.Estado).HasConversion<int>();

        constructor.Ignore(m => m.RecibeCitas);
        constructor.Ignore(m => m.CuposSemanales);

        constructor.HasOne(m => m.Especialidad)
            .WithMany()
            .HasForeignKey(m => m.EspecialidadId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(m => m.Sucursal)
            .WithMany()
            .HasForeignKey(m => m.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Las colecciones se exponen como IReadOnlyCollection; EF escribe sobre el campo privado.
        constructor.HasMany(m => m.Horarios)
            .WithOne()
            .HasForeignKey(h => h.MedicoId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasMany(m => m.Bloqueos)
            .WithOne()
            .HasForeignKey(b => b.MedicoId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.Navigation(m => m.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);
        constructor.Navigation(m => m.Bloqueos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
