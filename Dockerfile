#---------------- Compile .NET Application ----------------
# Get Base Image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Set Build Configurations
ARG configuration=Release
# Copy Code to Container
COPY . .
# Execute commands to build
RUN dotnet publish "ArenaGaming.Api/ArenaGaming.Api.csproj" -c $configuration -o /app /p:UseAppHost=false

#---------------- Run .NET Application ----------------
# Get Base Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# Set Runtime Configurations
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Create app directory and non-root user
RUN mkdir -p /app && \
    adduser --disabled-password --gecos "" appuser && \
    chown -R appuser:appuser /app

# Copy Build Code for Execution
COPY --from=build /app /app
RUN chown -R appuser:appuser /app

# Switch to non-root user and set working directory
USER appuser
WORKDIR /app

ENTRYPOINT ["dotnet", "ArenaGaming.Api.dll"]