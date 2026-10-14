FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion

WORKDIR /fuente

COPY Directory.Packages.props global.json RedDiff.slnx ./
COPY servidor/fuente/RedDiff.Dominio/RedDiff.Dominio.csproj servidor/fuente/RedDiff.Dominio/
COPY servidor/fuente/RedDiff.Aplicacion/RedDiff.Aplicacion.csproj servidor/fuente/RedDiff.Aplicacion/
COPY servidor/fuente/RedDiff.Infraestructura/RedDiff.Infraestructura.csproj servidor/fuente/RedDiff.Infraestructura/
COPY servidor/fuente/RedDiff.Api/RedDiff.Api.csproj servidor/fuente/RedDiff.Api/

RUN dotnet restore servidor/fuente/RedDiff.Api/RedDiff.Api.csproj

COPY servidor/fuente/ servidor/fuente/

RUN dotnet publish servidor/fuente/RedDiff.Api/RedDiff.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /aplicacion \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS ejecucion

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /aplicacion
COPY --from=compilacion --chown=1654:1654 /aplicacion ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
EXPOSE 5514/udp

USER $APP_UID

ENTRYPOINT ["dotnet", "RedDiff.Api.dll"]
