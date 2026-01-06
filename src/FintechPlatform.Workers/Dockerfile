FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["FintechPlatform.Workers/FintechPlatform.Workers.csproj", "FintechPlatform.Workers/"]
COPY ["domain/FintechPlatform.Domain.csproj", "domain/"]
COPY ["infrastructure/FintechPlatform.Infrastructure.csproj", "infrastructure/"]

# Restore dependencies
RUN dotnet restore "FintechPlatform.Workers/FintechPlatform.Workers.csproj"

# Copy source code
COPY . .
WORKDIR "/src/FintechPlatform.Workers"

# Build
RUN dotnet build "FintechPlatform.Workers.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "FintechPlatform.Workers.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5002

ENTRYPOINT ["dotnet", "FintechPlatform.Workers.dll"]
