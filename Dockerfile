# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files for optimized layer caching
COPY Delamujer.ProductCatalog.slnx ./
COPY src/Delamujer.ProductCatalog.Domain/Delamujer.ProductCatalog.Domain.csproj src/Delamujer.ProductCatalog.Domain/
COPY src/Delamujer.ProductCatalog.Application/Delamujer.ProductCatalog.Application.csproj src/Delamujer.ProductCatalog.Application/
COPY src/Delamujer.ProductCatalog.Infrastructure/Delamujer.ProductCatalog.Infrastructure.csproj src/Delamujer.ProductCatalog.Infrastructure/
COPY src/Delamujer.ProductCatalog.Api/Delamujer.ProductCatalog.Api.csproj src/Delamujer.ProductCatalog.Api/
COPY tests/Delamujer.ProductCatalog.UnitTests/Delamujer.ProductCatalog.UnitTests.csproj tests/Delamujer.ProductCatalog.UnitTests/
COPY tests/Delamujer.ProductCatalog.IntegrationTests/Delamujer.ProductCatalog.IntegrationTests.csproj tests/Delamujer.ProductCatalog.IntegrationTests/

# Restore dependencies
RUN dotnet restore src/Delamujer.ProductCatalog.Api/Delamujer.ProductCatalog.Api.csproj

# Copy remaining source code
COPY src/ src/

# Build and publish release artifacts
WORKDIR /source/src/Delamujer.ProductCatalog.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Minimal Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Delamujer.ProductCatalog.Api.dll"]
