# syntax=docker/dockerfile:1

# Build from the repository root:  docker build -t localfoundry-api .
# The model itself does NOT run in this container. Foundry Local runs on the host and the
# API reaches it over HTTP (see FoundryLocal__Endpoint below and the README).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, from only the files that affect it, so Docker reuses this layer until a
# package version or project file changes.
COPY Directory.Packages.props ./
COPY LocalFoundry.Api/LocalFoundry.Api.csproj LocalFoundry.Api/
RUN dotnet restore LocalFoundry.Api/LocalFoundry.Api.csproj

COPY LocalFoundry.Api/ LocalFoundry.Api/
RUN dotnet publish LocalFoundry.Api/LocalFoundry.Api.csproj -c Release --no-restore -o /app/publish

# The Foundry Local SDK package ships CUDA execution providers and debug symbols (~500 MB).
# This container never runs inference (Foundry Local on the host does), so they are dead weight.
RUN rm -f /app/publish/*cuda*.so /app/publish/*.dbg /app/publish/*.pdb

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Inside a container "localhost" is the container itself, so point at the host instead.
# host.docker.internal works out of the box on Docker Desktop; on Linux add
# --add-host=host.docker.internal:host-gateway (the compose file does this).
ENV FoundryLocal__Endpoint=http://host.docker.internal:5273/v1

# The ASP.NET Core base image listens on 8080 and provides this unprivileged user.
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "LocalFoundry.Api.dll"]
