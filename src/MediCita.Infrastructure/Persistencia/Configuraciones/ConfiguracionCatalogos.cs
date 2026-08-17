using MediCita.Domain.Agenda;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Comun;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCita.Infrastructure.Persistencia.Configuraciones;

public sealed class ConfiguracionEspecialidad : IEntityTypeConfiguration<Especialidad>
{
    public void Configure(EntityTypeBuilder<Especialidad> constructor)
    {
        constructor.ToTable("Especialidades");
        constructor.HasKey(e => e.Id);
        constructor.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        constructor.Property(e => e.Descripcion).HasMaxLength(300);
        constructor.HasIndex(e => e.Nombre).IsUnique();
    }
}

public sealed class ConfiguracionSucursal : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> constructor)
    {
        constructor.ToTable("Sucursales");
        constructor.HasKey(s => s.Id);
        constructor.Property(s => s.Nombre).HasMaxLength(100).IsRequired();
        constructor.Property(s => s.Direccion).HasMaxLength(250);
        constructor.Property(s => s.Telefono).HasMaxLength(25);
        constructor.HasIndex(s => s.Nombre).IsUnique();
    }
}

public sealed class ConfiguracionHorario : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> constructor)
    {
        constructor.ToTable("Horarios");
        constructor.HasKey(h => h.Id);
        constructor.Property(h => h.Dia).HasConversion<int>();
        constructor.Property(h => h.HoraInicio).HasColumnType("time");
        constructor.Property(h => h.HoraFin).HasColumnType("time");
        constructor.Ignore(h => h.CantidadDeCupos);
        constructor.HasIndex(h => new { h.MedicoId, h.Dia });
    }
}

public sealed class ConfiguracionBloqueo : IEntityTypeConfiguration<BloqueoAgenda>
{
    public void Configure(EntityTypeBuilder<BloqueoAgenda> constructor)
    {
        constructor.ToTable("BloqueosDeAgenda");
        constructor.HasKey(b => b.Id);
        constructor.Property(b => b.Motivo).HasMaxLength(200).IsRequired();
        constructor.HasIndex(b => new { b.MedicoId, b.Desde });
    }
}

public sealed class ConfiguracionBitacora : IEntityTypeConfiguration<RegistroActividad>
{
    public void Configure(EntityTypeBuilder<RegistroActividad> constructor)
    {
        constructor.ToTable("Bitacora");
        constructor.HasKey(r => r.Id);
        constructor.Property(r => r.Descripcion).HasMaxLength(300).IsRequired();
        constructor.Property(r => r.Categoria).HasConversion<int>();
        constructor.HasIndex(r => r.Momento).IsDescending();
    }
}

public sealed class ConfiguracionLatido : IEntityTypeConfiguration<LatidoDelWorker>
{
    public void Configure(EntityTypeBuilder<LatidoDelWorker> constructor)
    {
        constructor.ToTable("LatidosDelWorker");
        constructor.HasKey(l => l.Id);
        constructor.HasIndex(l => l.Momento).IsDescending();
    }
}
