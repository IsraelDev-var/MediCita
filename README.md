# MediCita — Sistema web de gestión de citas médicas

Implementación del proyecto descrito en el documento de arquitectura *"Modelo 4+1 vistas"*
(ITLA, Unidad V) y en el documento de mockups de interfaz.

**Integrantes:** Israel Vargas (2023-1861) · Anderson Calderón (2024-0986)

---

## Cómo correrlo

Requisitos: .NET SDK 8 o superior, Node 18+ y SQL Server LocalDB (o Docker).

```bash
dotnet ef database update -p src/MediCita.Infrastructure -s src/MediCita.Infrastructure
```

Luego, en tres terminales:

```bash
dotnet run --project src/MediCita.Api --urls http://localhost:5245
```

```bash
dotnet run --project src/MediCita.Worker
```

```bash
npm start --prefix src/MediCita.Web
```

- Aplicación: <http://localhost:4200>
- Swagger: <http://localhost:5245/swagger>

En el primer arranque en modo Desarrollo la API aplica las migraciones y carga los
datos de demostración de los mockups.

### Usuarios de demostración

Todos usan la contraseña `MediCita2026`:

| Rol | Correo |
| --- | --- |
| Paciente | `maria.pena@correo.do` |
| Médico | `laura.bencosme@medicita.do` |
| Administrador | `admin@medicita.do` |

En desarrollo los recordatorios no salen por SMTP: se escriben como archivos `.eml`
en `src/MediCita.Worker/bin/Debug/net8.0/correos-salida/`, que se abren con
cualquier cliente de correo.

---

## Las cuatro vistas, en el código

### Vista lógica — `src/MediCita.Domain`

| Concepto de la Figura 1 | Dónde vive |
| --- | --- |
| Herencia `Usuario` → `Paciente` / `Medico` / `Administrador` | `Usuarios/` |
| `Cita` con su enumeración de estados | `Citas/Cita.cs`, `Citas/EstadoCita.cs` |
| `Horario` y bloqueos de agenda | `Agenda/` |
| `Notificacion` con `Enviar()` polimórfico | `Notificaciones/` |

Patrones aplicados:

- **Repository** — `Application/Abstracciones/Repositorios.cs`; la implementación
  con EF Core está en `Infrastructure/Persistencia/Repositorios/`.
- **Observer** — `Cita` acumula sus cambios de estado y
  `PublicadorDeCambiosDeCita` los reparte entre `ProgramadorDeRecordatorios` y
  `BitacoraDeCitas`. Agregar una reacción nueva es registrar otro `ICitaObservador`.
- **Strategy** — `IEstrategiaDeCanal` con `CanalCorreoSmtp`, `CanalCorreoArchivo` y
  `CanalSmsSimulado`; `SelectorDeCanal` elige según el canal de la notificación.
- **Inyección de dependencias** — `RegistroDeAplicacion` y `RegistroDeInfraestructura`.

### Vista de desarrollo — organización en capas

```
MediCita.Domain          reglas de negocio; no depende de nadie
MediCita.Application     casos de uso; define las interfaces que necesita
MediCita.Infrastructure  EF Core, JWT, canales de envío; implementa esas interfaces
MediCita.Api             controladores REST
MediCita.Worker          proceso de recordatorios
MediCita.Web             SPA en Angular 17
```

La dependencia se invierte en la frontera Aplicación → Infraestructura, de modo que
el dominio nunca conoce Entity Framework ni ASP.NET.

### Vista de procesos

Cuatro procesos en ejecución: el navegador, la API (pool de hilos de Kestrel), el
worker (`TareaDeRecordatorios`, ciclo configurable en `Recordatorios:MinutosEntreCiclos`)
y SQL Server. **La API y el worker no se comunican entre sí**: el worker deja un
`LatidoDelWorker` en la base de datos y el panel de administración lo lee desde ahí.

### Vista física

`docker-compose.yml` levanta SQL Server 2022 y los contenedores de API y worker por
separado, tal como la Figura 4. En producción el correo sale por SMTP (`Correo:Modo = Smtp`).

---

## Los dos escenarios del documento

**Escenario 1 — Agendar una cita.** `POST /api/citas` → `ServicioCitas.AgendarAsync`
valida el cupo contra el `Horario` del médico, comprueba que siga libre, crea la
`Cita` en estado Pendiente y publica el cambio; el observador programa el
recordatorio. La base de datos refuerza la integridad con el índice único filtrado
`IX_Citas_Cupo_Unico`, así que dos peticiones simultáneas terminan en un 409 y no en
una doble reserva.

**Escenario 2 — Recordatorio automático.** El worker despierta cada N minutos,
toma las notificaciones vencidas, arma el mensaje con la estrategia del canal y las
marca como enviadas. Si el envío falla, la notificación queda *Fallida* y se
reintenta en el próximo ciclo hasta el máximo configurado: la API nunca se bloquea
por el correo.

---

## Pantallas

| Mockup | Ruta | Rol |
| --- | --- | --- |
| 01 · Acceso y registro | `/acceso` | público |
| 02 · Agendar cita | `/citas/nueva` | Paciente |
| 03 · Confirmación | modal de `/citas/nueva` | Paciente |
| 04 · Mis citas | `/citas` | Paciente |
| 05 · Agenda diaria | `/medico/agenda` | Médico |
| 06 · Panel de administración | `/admin` | Administrador |
| 07 · Correo de recordatorio | `PlantillaCorreoRecordatorio` | Sistema |

---

## Pruebas

```bash
dotnet test
```

65 pruebas unitarias sobre las reglas del dominio (estados de la cita, generación de
cupos, polimorfismo de las notificaciones) y sobre los servicios de aplicación
(doble reserva, reprogramación, anulación del recordatorio, ciclo del worker y
control de acceso por rol).

---

## Configuración

| Clave | Para qué |
| --- | --- |
| `ConnectionStrings:MediCita` | Cadena de conexión a SQL Server |
| `Jwt:Clave` | Clave de firma del token (mínimo 32 caracteres) |
| `Correo:Modo` | `Archivo` en desarrollo, `Smtp` en producción |
| `Recordatorios:MinutosEntreCiclos` | Cada cuánto despierta el worker |
| `Recordatorios:UrlAplicacion` | Base de los enlaces del correo |

Los valores de producción se leen de variables de entorno o *user-secrets*; el
repositorio solo trae los de desarrollo local. La API se niega a arrancar si
`Jwt:Clave` no está configurada.
