FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["api/FintechPlatform.Api.csproj", "api/"]
COPY ["domain/FintechPlatform.Domain.csproj", "domain/"]
COPY ["infrastructure/FintechPlatform.Infrastructure.csproj", "infrastructure/"]

# Restore dependencies
RUN dotnet restore "api/FintechPlatform.Api.csproj"

# Copy source code
COPY . .
WORKDIR "/src/api"

# Build
RUN dotnet build "FintechPlatform.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "FintechPlatform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 5153

ENTRYPOINT ["dotnet", "FintechPlatform.Api.dll"]
