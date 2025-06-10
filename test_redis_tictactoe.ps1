#!/usr/bin/env pwsh

Write-Host "🔥 TESTING REDIS-ONLY TIC-TAC-TOE API 🔥" -ForegroundColor Red
Write-Host "===============================================" -ForegroundColor Yellow

$baseUrl = "http://localhost:5000"
$playerId = "b0fb5a60-501f-401a-969b-55208f9064a1"

Write-Host ""
Write-Host "🏥 1. HEALTH CHECK (Redis-only)" -ForegroundColor Green
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method GET
    Write-Host "Status: $($healthResponse.Status)" -ForegroundColor Cyan
    Write-Host "Services:" -ForegroundColor Cyan
    foreach ($service in $healthResponse.Services) {
        Write-Host "  - $($service.Service): $($service.Status)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ HEALTH CHECK FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the API is running on $baseUrl" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "🎮 2. CREATE SESSION" -ForegroundColor Green
$sessionBody = @{
    playerId = $playerId
} | ConvertTo-Json

$sessionResponse = Invoke-RestMethod -Uri "$baseUrl/api/sessions" -Method POST -Body $sessionBody -ContentType "application/json"
$sessionId = $sessionResponse.id
Write-Host "Session ID: $sessionId" -ForegroundColor Cyan

Write-Host ""
Write-Host "🎯 3. INITIALIZE GAME" -ForegroundColor Green
$gameResponse = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/init-game" -Method POST
$gameId = $gameResponse.id
Write-Host "Game ID: $gameId" -ForegroundColor Cyan
Write-Host "Board: '$($gameResponse.board)'" -ForegroundColor Cyan
Write-Host "Board Length: $($gameResponse.board.Length)" -ForegroundColor Yellow

Write-Host ""
Write-Host "🎲 4. MAKE FIRST MOVE (Position 0)" -ForegroundColor Green
$moveBody = @{
    playerId = $playerId
    position = 0
} | ConvertTo-Json

try {
    $moveResponse = Invoke-RestMethod -Uri "$baseUrl/api/games/$gameId/moves" -Method POST -Body $moveBody -ContentType "application/json"
    Write-Host "✅ MOVE SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Board after move: '$($moveResponse.board)'" -ForegroundColor Cyan
    Write-Host "Current player: $($moveResponse.currentPlayerSymbol)" -ForegroundColor Cyan
    Write-Host "Status: $($moveResponse.status)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ MOVE FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Get detailed error
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🔍 5. GET GAME STATE" -ForegroundColor Green
try {
    $gameState = Invoke-RestMethod -Uri "$baseUrl/api/games/$gameId" -Method GET
    Write-Host "Game Status: $($gameState.status)" -ForegroundColor Cyan
    Write-Host "Board: '$($gameState.board)'" -ForegroundColor Cyan
    Write-Host "Board Length: $($gameState.board.Length)" -ForegroundColor Yellow
    Write-Host "Current Player: $($gameState.currentPlayerSymbol)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ GET GAME FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎲 6. MAKE SECOND MOVE (Position 1)" -ForegroundColor Green
$moveBody2 = @{
    playerId = $playerId
    position = 1
} | ConvertTo-Json

try {
    $moveResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/games/$gameId/moves" -Method POST -Body $moveBody2 -ContentType "application/json"
    Write-Host "✅ SECOND MOVE SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Board after move: '$($moveResponse2.board)'" -ForegroundColor Cyan
    Write-Host "Current player: $($moveResponse2.currentPlayerSymbol)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ SECOND MOVE FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🏁 REDIS TIC-TAC-TOE TEST COMPLETE!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Yellow