using System.Text;
using MediCita.Api.Middleware;
using MediCita.Api.Seguridad;
using MediCita.Application;
using MediCita.Application.Abstracciones;
using MediCita.Infrastructure;
using MediCita.Infrastructure.Persistencia;
using MediCita.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var constructor = WebApplication.CreateBuilder(args);

const string PoliticaCors = "MediCitaWeb";

constructor.Services.AddControllers();
constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<IUsuarioActual, UsuarioActual>();

constructor.Services.AgregarAplicacion();
constructor.Services.AgregarInfraestructura(constructor.Configuration);

// --- Autenticación por token JWT ------------------------------------------------
var opcionesJwt = constructor.Configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();

if (string.IsNullOrWhiteSpace(opcionesJwt.Clave) || opcionesJwt.Clave.Length < 32)
{
    throw new InvalidOperationException(
        "Falta configurar Jwt:Clave con al menos 32 caracteres. " +
        "En desarrollo se toma de appsettings.Development.json; en producción, de variables de entorno o user-secrets.");
}

constructor.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = opcionesJwt.Emisor,
            ValidAudience = opcionesJwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesJwt.Clave)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

constructor.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Paciente, p => p.RequireRole(Politicas.Paciente))
    .AddPolicy(Politicas.Medico, p => p.RequireRole(Politicas.Medico))
    .AddPolicy(Politicas.Administrador, p => p.RequireRole(Politicas.Administrador));

// --- CORS para el frontend Angular ----------------------------------------------
var origenes = constructor.Configuration.GetSection("Cors:Origenes").Get<string[]>()
               ?? ["http://localhost:4200"];

constructor.Services.AddCors(opciones =>
    opciones.AddPolicy(PoliticaCors, politica => politica
        .WithOrigins(origenes)
        .AllowAnyHeader()
        .AllowAnyMethod()));

constructor.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MediCita API",
        Version = "v1",
        Description = "API REST de gestión de citas médicas. Roles: Paciente, Médico y Administrador."
    });

    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue aquí el token devuelto por /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    opciones.AddSecurityDefinition("Bearer", esquema);
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

var app = constructor.Build();

app.UseMiddleware<MiddlewareDeErrores>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "MediCita API");

    // En desarrollo la API deja la base al día y carga los datos de los mockups.
    using var alcance = app.Services.CreateScope();
    var contexto = alcance.ServiceProvider.GetRequiredService<MediCitaDbContext>();
    await contexto.Database.MigrateAsync();
    await alcance.ServiceProvider.GetRequiredService<SembradorDeDatos>().SembrarAsync();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/salud", () => Results.Ok(new { estado = "Operativa", momento = DateTime.Now }))
   .WithTags("Sistema");

app.Run();

/// <summary>Expuesto para las pruebas de integración.</summary>
public partial class Program;
