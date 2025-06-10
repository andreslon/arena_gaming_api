#!/usr/bin/env pwsh

Write-Host "🔥 INICIANDO API CON BASE DE DATOS LIMPIA..." -ForegroundColor Red
Write-Host "=============================================" -ForegroundColor Red

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=arena_gaming;Username=postgres;Password=admin"
$env:ConnectionStrings__Redis = "localhost:6379"
$env:ConnectionStrings__Pulsar = "pulsar://localhost:6650"
$env:Gemini__ApiKey = "fake_key"

# Change to API directory
Set-Location -Path "ArenaGaming.Api"

Write-Host "🚀 Iniciando API que aplicará migraciones automáticamente..." -ForegroundColor Yellow

# Run the API (it will apply migrations automatically on startup)
dotnet run 