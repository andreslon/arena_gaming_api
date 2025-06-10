# =====================================================
# ARENA GAMING API - SCRIPT COMPLETO DE PRUEBAS
# Ejecuta: .\test_endpoints_final.ps1
# =====================================================

param(
    [switch]$SkipAPIStart,
    [string]$Port = "5000"
)

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:$Port/api"

# Colores para output
function Write-Success { param($Message) Write-Host "✅ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "ℹ️  $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠️  $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "❌ $Message" -ForegroundColor Red }
function Write-Title { param($Message) Write-Host "`n🚀 $Message" -ForegroundColor Magenta -BackgroundColor Black }

# Función para hacer requests
function Invoke-APIRequest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [string]$Body = $null,
        [string]$Description
    )
    
    $url = "$baseUrl$Endpoint"
    Write-Info "Testing: $Description"
    Write-Host "   Method: $Method" -ForegroundColor Gray
    Write-Host "   URL: $url" -ForegroundColor Gray
    
    if ($Body) {
        Write-Host "   Body: $Body" -ForegroundColor Gray
    }
    
    try {
        $headers = @{ "Content-Type" = "application/json" }
        
        if ($Body) {
            $response = Invoke-RestMethod -Uri $url -Method $Method -Body $Body -Headers $headers -TimeoutSec 30
        } else {
            $response = Invoke-RestMethod -Uri $url -Method $Method -TimeoutSec 30
        }
        
        Write-Success "SUCCESS"
        Write-Host "Response:" -ForegroundColor Yellow
        
        if ($response) {
            $jsonResponse = $response | ConvertTo-Json -Depth 5
            Write-Host $jsonResponse -ForegroundColor White
        } else {
            Write-Host "Empty response" -ForegroundColor Gray
        }
        
        return $response
    }
    catch {
        Write-Error "FAILED: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
        return $null
    }
}

# Función para esperar a que la API esté lista
function Wait-ForAPI {
    param([int]$MaxAttempts = 20)
    
    Write-Info "Waiting for API to be ready..."
    
    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            $response = Invoke-RestMethod -Uri "$baseUrl/health/status" -Method GET -TimeoutSec 5
            Write-Success "API is ready!"
            return $true
        }
        catch {
            Write-Host "Attempt $i/$MaxAttempts - API not ready yet..." -ForegroundColor Yellow
            Start-Sleep -Seconds 3
        }
    }
    
    Write-Error "API failed to start after $MaxAttempts attempts"
    return $false
}

# INICIO DEL SCRIPT
Write-Title "ARENA GAMING API - COMPREHENSIVE TEST SCRIPT"
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host "Skip API Start: $SkipAPIStart" -ForegroundColor Cyan

# 1. LEVANTAR LA API (si no se especifica -SkipAPIStart)
if (-not $SkipAPIStart) {
    Write-Title "STEP 1: STARTING API"
    
    # Matar procesos existentes
    Write-Info "Killing existing dotnet processes..."
    Get-Process | Where-Object {$_.ProcessName -eq "dotnet"} | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # Navegar al directorio correcto
    $apiPath = "D:\Andreslon\arena_gaming_api\ArenaGaming.Api"
    if (Test-Path $apiPath) {
        Set-Location $apiPath
        Write-Info "Changed to API directory: $apiPath"
    } else {
        Write-Error "API directory not found: $apiPath"
        exit 1
    }
    
    # Configurar variables de entorno
    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    
    # Iniciar la API en background
    Write-Info "Starting API on port $Port..."
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -WindowStyle Hidden -PassThru
    
    # Esperar a que la API esté lista
    if (-not (Wait-ForAPI)) {
        Write-Error "Failed to start API"
        exit 1
    }
} else {
    Write-Info "Skipping API startup (assuming it's already running)"
    if (-not (Wait-ForAPI -MaxAttempts 3)) {
        Write-Error "API is not running. Remove -SkipAPIStart flag to start it automatically."
        exit 1
    }
}

# 2. HEALTH CHECKS
Write-Title "STEP 2: HEALTH CHECKS"

Invoke-APIRequest -Method "GET" -Endpoint "/health/status" -Description "Basic Health Check"
Invoke-APIRequest -Method "GET" -Endpoint "/health/postgresql" -Description "PostgreSQL Health"
Invoke-APIRequest -Method "GET" -Endpoint "/health/redis" -Description "Redis Health"
Invoke-APIRequest -Method "GET" -Endpoint "/health/pulsar" -Description "Pulsar Health"

# 3. GAME LOGIC TEST
Write-Title "STEP 3: GAME LOGIC TEST"

$gameLogicResponse = Invoke-APIRequest -Method "GET" -Endpoint "/test/game-logic" -Description "Test Game Logic"

# 4. SESSION AND GAME FLOW
Write-Title "STEP 4: SESSION AND GAME FLOW"

# Generar Player ID
$playerId = [System.Guid]::NewGuid().ToString()
Write-Info "Generated Player ID: $playerId"

# 4.1 Crear Sesión
$sessionBody = @{
    playerId = $playerId
} | ConvertTo-Json

$sessionResponse = Invoke-APIRequest -Method "POST" -Endpoint "/sessions" -Body $sessionBody -Description "Create Session"

if ($sessionResponse -and $sessionResponse.id) {
    $sessionId = $sessionResponse.id
    Write-Success "Session created with ID: $sessionId"
    
    # 4.2 Iniciar Juego
    $gameResponse = Invoke-APIRequest -Method "POST" -Endpoint "/sessions/$sessionId/init-game" -Description "Initialize Game"
    
    if ($gameResponse -and $gameResponse.data -and $gameResponse.data.id) {
        $gameId = $gameResponse.data.id
        Write-Success "Game started with ID: $gameId"
        
        # 4.3 Hacer primer movimiento
        $move1Body = @{
            playerId = $playerId
            position = 0
        } | ConvertTo-Json
        
        Invoke-APIRequest -Method "POST" -Endpoint "/games/$gameId/moves" -Body $move1Body -Description "First Move (Position 0)"
        
        # 4.4 Hacer segundo movimiento
        $move2Body = @{
            playerId = $playerId
            position = 1
        } | ConvertTo-Json
        
        Invoke-APIRequest -Method "POST" -Endpoint "/games/$gameId/moves" -Body $move2Body -Description "Second Move (Position 1)"
        
        # 4.5 Intentar movimiento duplicado (debe fallar)
        Invoke-APIRequest -Method "POST" -Endpoint "/games/$gameId/moves" -Body $move1Body -Description "Duplicate Move (Should Fail)"
        
        # 4.6 Obtener estado del juego
        Invoke-APIRequest -Method "GET" -Endpoint "/games/$gameId" -Description "Get Game State"
        
        # 4.7 Movimiento de IA
        Invoke-APIRequest -Method "POST" -Endpoint "/sessions/$sessionId/ai-move" -Description "AI Move"
        
    } else {
        Write-Warning "Could not start game - skipping game moves"
    }
} else {
    Write-Warning "Could not create session - skipping game flow"
}

# 5. NOTIFICATION TESTS
Write-Title "STEP 5: NOTIFICATION TESTS"

# 5.1 Test notification
Invoke-APIRequest -Method "POST" -Endpoint "/notifications/test" -Description "Test Notification"

# 5.2 User-specific notification
$userNotificationBody = @{
    title = "API Test Notification"
    message = "This is a test notification from PowerShell script"
    type = 1
} | ConvertTo-Json

Invoke-APIRequest -Method "POST" -Endpoint "/notifications/user/testuser123" -Body $userNotificationBody -Description "User Notification"

# 5.3 Broadcast notification
$broadcastBody = @{
    title = "Broadcast Test"
    message = "This is a broadcast test message"
    type = 2
} | ConvertTo-Json

Invoke-APIRequest -Method "POST" -Endpoint "/notifications/broadcast" -Body $broadcastBody -Description "Broadcast Notification"

# 6. NOTIFICATION PREFERENCES
Write-Title "STEP 6: NOTIFICATION PREFERENCES"

# 6.1 Get preferences
Invoke-APIRequest -Method "GET" -Endpoint "/notificationpreferences/testuser123" -Description "Get Notification Preferences"

# 6.2 Update preferences
$preferencesBody = @{
    gameEvents = $true
    socialEvents = $true
    soundEffects = $false
    volume = 75
    emailNotifications = $true
    pushNotifications = $true
    tournamentAlerts = $false
    playerActions = $true
    systemUpdates = $true
} | ConvertTo-Json

Invoke-APIRequest -Method "PUT" -Endpoint "/notificationpreferences/testuser123" -Body $preferencesBody -Description "Update Notification Preferences"

# 6.3 Reset preferences
Invoke-APIRequest -Method "POST" -Endpoint "/notificationpreferences/testuser123/reset" -Description "Reset Notification Preferences"

# RESUMEN FINAL
Write-Title "TEST SUMMARY"
Write-Success "All API endpoints have been tested!"
Write-Info "API is running on: $baseUrl"
Write-Info "Player ID used: $playerId"

if ($sessionResponse) {
    Write-Info "Session ID: $($sessionResponse.id)"
}

Write-Host "`n" -NoNewline
Write-Host "🎯 QUICK CURL COMMANDS FOR MANUAL TESTING:" -ForegroundColor Yellow
Write-Host "curl -X GET `"$baseUrl/health/status`"" -ForegroundColor White
Write-Host "curl -X GET `"$baseUrl/test/game-logic`"" -ForegroundColor White
Write-Host "curl -X POST `"$baseUrl/sessions`" -H `"Content-Type: application/json`" -d `"{\\`"playerId\\`":\\`"$playerId\\`"}`"" -ForegroundColor White

Write-Host "`n🔥 KEEP THE API RUNNING FOR FURTHER TESTING!" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the script (API will continue running)" -ForegroundColor Yellow

# Mantener el script activo para que la API siga corriendo
try {
    Write-Host "`nWaiting... (Press Ctrl+C to exit)" -ForegroundColor Cyan
    while ($true) {
        Start-Sleep -Seconds 10
        # Verificar que la API sigue corriendo
        try {
            Invoke-RestMethod -Uri "$baseUrl/health/status" -Method GET -TimeoutSec 5 | Out-Null
        } catch {
            Write-Warning "API seems to have stopped"
            break
        }
    }
} catch {
    Write-Info "Script interrupted by user"
} finally {
    Write-Host "`n✅ Script completed!" -ForegroundColor Green
} 