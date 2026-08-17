using MediCita.Domain.Agenda;
using MediCita.Domain.Catalogos;
using MediCita.Domain.Citas;
using MediCita.Domain.Comun;
using MediCita.Domain.Notificaciones;
using MediCita.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace MediCita.Infrastructure.Persistencia;

/// <summary>
/// Contexto de Entity Framework Core. Es el detalle de persistencia que la capa
/// de aplicación desconoce: solo la infraestructura lo referencia.
/// </summary>
public sealed class MediCitaDbContext : DbContext
{
    public const string NombreSecuenciaCita = "SecuenciaCita";
    public const string NombreIndiceCupo = "IX_Citas_Cupo_Unico";

    public MediCitaDbContext(DbContextOptions<MediCitaDbContext> opciones) : base(opciones) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<BloqueoAgenda> Bloqueos => Set<BloqueoAgenda>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<RegistroActividad> Bitacora => Set<RegistroActividad>();
    public DbSet<LatidoDelWorker> LatidosDelWorker => Set<LatidoDelWorker>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // Los tipos derivados que no tienen DbSet propio se declaran para que EF
        // los incluya en la jerarquía antes de configurar el discriminador.
        modelo.Entity<Administrador>();
        modelo.Entity<NotificacionCorreo>();
        modelo.Entity<NotificacionSms>();

        modelo.ApplyConfigurationsFromAssembly(typeof(MediCitaDbContext).Assembly);
        // Arranca en 700 para que los códigos se parezcan a los del diseño ("2026-0731").
        modelo.HasSequence<int>(NombreSecuenciaCita).StartsAt(700).IncrementsBy(1);
    }
}
