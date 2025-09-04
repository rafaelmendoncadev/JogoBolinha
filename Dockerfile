# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["JogoBolinha/JogoBolinha.csproj", "JogoBolinha/"]
COPY ["JogoBolinha.Tests/JogoBolinha.Tests.csproj", "JogoBolinha.Tests/"]
RUN dotnet restore "JogoBolinha/JogoBolinha.csproj"

# Copy the source code and build
COPY . .
WORKDIR "/src/JogoBolinha"
RUN dotnet build "JogoBolinha.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "JogoBolinha.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install necessary packages for SQLite
RUN apt-get update && apt-get install -y \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Create directory for database
RUN mkdir -p /app/data

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/jogabolinha.db"

# Expose port (Railway will set the PORT environment variable)
EXPOSE $PORT

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "JogoBolinha.dll"]