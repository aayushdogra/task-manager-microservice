# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY TaskManager.csproj ./
RUN dotnet restore

# Copy everything else
COPY . .

# Publish app
RUN dotnet publish -c Release -o /app/publish --no-restore


# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl (for healthcheck)
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

# Expose ports
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Copy published output
COPY --from=build /app/publish .

# Run
ENTRYPOINT ["dotnet", "TaskManager.dll"]