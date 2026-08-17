# Contenedor del worker de recordatorios: proceso separado de la API, de modo que
# una caída suya no afecta al agendamiento de citas.
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MediCita.Domain/MediCita.Domain.csproj src/MediCita.Domain/
COPY src/MediCita.Application/MediCita.Application.csproj src/MediCita.Application/
COPY src/MediCita.Infrastructure/MediCita.Infrastructure.csproj src/MediCita.Infrastructure/
COPY src/MediCita.Worker/MediCita.Worker.csproj src/MediCita.Worker/
RUN dotnet restore src/MediCita.Worker/MediCita.Worker.csproj

COPY src/ src/
RUN dotnet publish src/MediCita.Worker/MediCita.Worker.csproj -c Release -o /app/publicado

FROM base AS final
WORKDIR /app
COPY --from=build /app/publicado .
ENTRYPOINT ["dotnet", "MediCita.Worker.dll"]
