# Contenedor de la API (vista física: corre detrás del proxy inverso Nginx).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MediCita.Domain/MediCita.Domain.csproj src/MediCita.Domain/
COPY src/MediCita.Application/MediCita.Application.csproj src/MediCita.Application/
COPY src/MediCita.Infrastructure/MediCita.Infrastructure.csproj src/MediCita.Infrastructure/
COPY src/MediCita.Api/MediCita.Api.csproj src/MediCita.Api/
RUN dotnet restore src/MediCita.Api/MediCita.Api.csproj

COPY src/ src/
RUN dotnet publish src/MediCita.Api/MediCita.Api.csproj -c Release -o /app/publicado

FROM base AS final
WORKDIR /app
COPY --from=build /app/publicado .
ENTRYPOINT ["dotnet", "MediCita.Api.dll"]
