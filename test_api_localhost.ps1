# Arena Gaming API - Test completo en localhost:5001
# Este script levanta la API y prueba todos los endpoints

Write-Host "LEVANTANDO Y PROBANDO ARENA GAMING API EN LOCALHOST:5001" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5001/api"
$playerId = [System.Guid]::NewGuid().ToString()

Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host "Player ID: $playerId" -ForegroundColor Yellow

# Paso 1: Levantar la API
Write-Host "`nLEVANTANDO API..." -ForegroundColor Green

# Matar procesos existentes
Get-Process | Where-Object {$_.ProcessName -like "*ArenaGaming*"} | Stop-Process -Force -ErrorAction SilentlyContinue

# Configurar variables de entorno
$env:ASPNETCORE_URLS = "http://localhost:5001"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Cambiar al directorio de la API si no estamos ahi
$apiPath = "D:\Andreslon\arena_gaming_api\ArenaGaming.Api"
if ((Get-Location).Path -ne $apiPath) {
    Set-Location $apiPath
}

# Iniciar la API en background
Write-Host "Iniciando API en background..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 15  # Esperar a que inicie

# Verificar que la API esta corriendo
Write-Host "Verificando que la API este corriendo..." -ForegroundColor Yellow
$attempts = 0
$maxAttempts = 10
$apiRunning = $false

while ($attempts -lt $maxAttempts -and -not $apiRunning) {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/health/status" -Method GET -TimeoutSec 5
        $apiRunning = $true
        Write-Host "API esta corriendo!" -ForegroundColor Green
    }
    catch {
        $attempts++
        Write-Host "Intento $attempts/$maxAttempts - Esperando..." -ForegroundColor Yellow
        Start-Sleep -Seconds 3
    }
}

if (-not $apiRunning) {
    Write-Host "No se pudo levantar la API" -ForegroundColor Red
    exit 1
}

# Funcion para hacer requests y mostrar responses
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [hashtable]$Headers = @{}
    )
    
    Write-Host "`n$Name" -ForegroundColor Cyan
    Write-Host "   URL: $Method $Url" -ForegroundColor Gray
    
    if ($Body) {
        Write-Host "   Body: $Body" -ForegroundColor Gray
    }
    
    try {
        if ($Body) {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Body $Body -ContentType "application/json" -Headers $Headers
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers
        }
        
        Write-Host "SUCCESS" -ForegroundColor Green
        Write-Host "Response:" -ForegroundColor Yellow
        $response | ConvertTo-Json -Depth 10 | Write-Host
        return $response
    }
    catch {
        Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            try {
                $errorBody = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorBody)
                $errorText = $reader.ReadToEnd()
                Write-Host "Error Response: $errorText" -ForegroundColor Red
            }
            catch {
                Write-Host "No se pudo leer el error response" -ForegroundColor Red
            }
        }
        return $null
    }
}

# Paso 2: Probar endpoints
Write-Host "`nPROBANDO ENDPOINTS..." -ForegroundColor Green

# Health Check basico
$healthResponse = Test-Endpoint -Name "HEALTH CHECK STATUS" -Method "GET" -Url "$baseUrl/health/status"

# Test Game Logic
$gameLogicResponse = Test-Endpoint -Name "TEST GAME LOGIC" -Method "GET" -Url "$baseUrl/test/game-logic"

# Health Checks especificos
Test-Endpoint -Name "HEALTH CHECK POSTGRESQL" -Method "GET" -Url "$baseUrl/health/postgresql"
Test-Endpoint -Name "HEALTH CHECK REDIS" -Method "GET" -Url "$baseUrl/health/redis"
Test-Endpoint -Name "HEALTH CHECK PULSAR" -Method "GET" -Url "$baseUrl/health/pulsar"

# Crear Sesion
$sessionBody = @{
    playerId = $playerId
} | ConvertTo-Json

$sessionResponse = Test-Endpoint -Name "CREAR SESION" -Method "POST" -Url "$baseUrl/sessions" -Body $sessionBody

if ($sessionResponse -and $sessionResponse.id) {
    $sessionId = $sessionResponse.id
    Write-Host "`nSession ID obtenido: $sessionId" -ForegroundColor Green
    
    # Iniciar Juego
    $gameResponse = Test-Endpoint -Name "INICIAR JUEGO" -Method "POST" -Url "$baseUrl/sessions/$sessionId/init-game"
    
    if ($gameResponse -and $gameResponse.data -and $gameResponse.data.id) {
        $gameId = $gameResponse.data.id
        Write-Host "`nGame ID obtenido: $gameId" -ForegroundColor Green
        
        # Primer movimiento
        $move1Body = @{
            playerId = $playerId
            position = 0
        } | ConvertTo-Json
        
        Test-Endpoint -Name "PRIMER MOVIMIENTO (Posicion 0)" -Method "POST" -Url "$baseUrl/games/$gameId/moves" -Body $move1Body
        
        # Segundo movimiento
        $move2Body = @{
            playerId = $playerId
            position = 1
        } | ConvertTo-Json
        
        Test-Endpoint -Name "SEGUNDO MOVIMIENTO (Posicion 1)" -Method "POST" -Url "$baseUrl/games/$gameId/moves" -Body $move2Body
        
        # Movimiento duplicado (debe fallar)
        Test-Endpoint -Name "MOVIMIENTO DUPLICADO (debe fallar)" -Method "POST" -Url "$baseUrl/games/$gameId/moves" -Body $move1Body
        
        # Obtener estado del juego
        Test-Endpoint -Name "OBTENER ESTADO DEL JUEGO" -Method "GET" -Url "$baseUrl/games/$gameId"
        
        # Movimiento de IA
        Test-Endpoint -Name "MOVIMIENTO DE IA" -Method "POST" -Url "$baseUrl/sessions/$sessionId/ai-move"
    }
}

# Probar notificaciones
Write-Host "`nPROBANDO NOTIFICACIONES..." -ForegroundColor Green

# Notificacion de prueba
Test-Endpoint -Name "NOTIFICACION DE PRUEBA" -Method "POST" -Url "$baseUrl/notifications/test"

# Notificacion a usuario especifico
$userNotificationBody = @{
    title = "Test Notification"
    message = "This is a test message"
    type = 1
} | ConvertTo-Json

Test-Endpoint -Name "NOTIFICACION A USUARIO" -Method "POST" -Url "$baseUrl/notifications/user/testuser123" -Body $userNotificationBody

# Notificacion broadcast
$broadcastBody = @{
    title = "Broadcast Test"
    message = "This is a broadcast message"
    type = 2
} | ConvertTo-Json

Test-Endpoint -Name "NOTIFICACION BROADCAST" -Method "POST" -Url "$baseUrl/notifications/broadcast" -Body $broadcastBody

# Probar preferencias de notificaciones
Write-Host "`nPROBANDO PREFERENCIAS..." -ForegroundColor Green

# Obtener preferencias
Test-Endpoint -Name "OBTENER PREFERENCIAS" -Method "GET" -Url "$baseUrl/notificationpreferences/testuser123"

# Actualizar preferencias
$preferencesBody = @{
    gameEvents = $true
    socialEvents = $true
    soundEffects = $false
    volume = 75
} | ConvertTo-Json

Test-Endpoint -Name "ACTUALIZAR PREFERENCIAS" -Method "PUT" -Url "$baseUrl/notificationpreferences/testuser123" -Body $preferencesBody

Write-Host "`nTESTING COMPLETADO!" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Cyan

# Mantener la API corriendo por un rato
Write-Host "`nManteniendo API corriendo por 60 segundos mas..." -ForegroundColor Yellow
Write-Host "Presiona Ctrl+C para terminar antes" -ForegroundColor Yellow

try {
    Start-Sleep -Seconds 60
} finally {
    # Limpiar: matar el proceso de la API
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Host "`nCerrando API..." -ForegroundColor Yellow
        $apiProcess.Kill()
    }
    
    # Matar cualquier proceso dotnet relacionado
    Get-Process | Where-Object {$_.ProcessName -eq "dotnet" -and $_.StartTime -gt (Get-Date).AddMinutes(-5)} | Stop-Process -Force -ErrorAction SilentlyContinue
}

Write-Host "Script completado!" -ForegroundColor Green 