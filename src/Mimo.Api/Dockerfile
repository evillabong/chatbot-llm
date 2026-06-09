# ── Etapa 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias primero (cache layer)
COPY ["src/Mimo.Api/Mimo.Api.csproj",            "src/Mimo.Api/"]
COPY ["src/Mimo.Core/Mimo.Core.csproj",          "src/Mimo.Core/"]
COPY ["src/Mimo.Infrastructure/Mimo.Infrastructure.csproj", "src/Mimo.Infrastructure/"]

RUN dotnet restore "src/Mimo.Api/Mimo.Api.csproj"

# Copiar código fuente completo y compilar
COPY . .
RUN dotnet publish "src/Mimo.Api/Mimo.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Etapa 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuario no-root por seguridad
RUN addgroup --system mimo && adduser --system --ingroup mimo mimo
USER mimo

COPY --from=build /app/publish .

# Puerto interno (Kestrel escucha en 8080 en imágenes base de .NET 8+)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Mimo.Api.dll"]
