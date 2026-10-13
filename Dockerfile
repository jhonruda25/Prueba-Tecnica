# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project definitions first for optimal layer caching
COPY Delamujer.ProductCatalog.slnx ./
COPY src/Delamujer.ProductCatalog.Domain/Delamujer.ProductCatalog.Domain.csproj src/Delamujer.ProductCatalog.Domain/
COPY src/Delamujer.ProductCatalog.Application/Delamujer.ProductCatalog.Application.csproj src/Delamujer.ProductCatalog.Application/
COPY src/Delamujer.ProductCatalog.Infrastructure/Delamujer.ProductCatalog.Infrastructure.csproj src/Delamujer.ProductCatalog.Infrastructure/
COPY src/Delamujer.ProductCatalog.Api/Delamujer.ProductCatalog.Api.csproj src/Delamujer.ProductCatalog.Api/
COPY tests/Delamujer.ProductCatalog.UnitTests/Delamujer.ProductCatalog.UnitTests.csproj tests/Delamujer.ProductCatalog.UnitTests/
COPY tests/Delamujer.ProductCatalog.IntegrationTests/Delamujer.ProductCatalog.IntegrationTests.csproj tests/Delamujer.ProductCatalog.IntegrationTests/

# Restore dependencies
RUN dotnet restore src/Delamujer.ProductCatalog.Api/Delamujer.ProductCatalog.Api.csproj

# Copy source code
COPY src/ src/

# Publish optimized Release build
WORKDIR /source/src/Delamujer.ProductCatalog.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Hardened, Ultra-minimal Runtime Image on Alpine Linux
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Security: Install icu-libs and tzdata for full globalization and timezone support
RUN apk add --no-cache icu-libs tzdata

# Security: Enable full globalization support
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Copy compiled artifacts with ownership assigned to unprivileged user
COPY --from=build --chown=app:app /app/publish .

# Security Best Practice: Run as non-root unprivileged user 'app' (UID 1654)
USER $APP_UID

ENTRYPOINT ["dotnet", "Delamujer.ProductCatalog.Api.dll"]
