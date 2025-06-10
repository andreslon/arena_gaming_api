Write-Host "TESTING REDIS-ONLY TIC-TAC-TOE API" -ForegroundColor Red

$baseUrl = "http://localhost:5000"
$playerId = "b0fb5a60-501f-401a-969b-55208f9064a1"

Write-Host "1. HEALTH CHECK" -ForegroundColor Green
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method GET
    Write-Host "Status: $($healthResponse.Status)" -ForegroundColor Cyan
} catch {
    Write-Host "HEALTH CHECK FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "2. CREATE SESSION" -ForegroundColor Green
$sessionBody = @{
    playerId = $playerId
} | ConvertTo-Json

$sessionResponse = Invoke-RestMethod -Uri "$baseUrl/api/sessions" -Method POST -Body $sessionBody -ContentType "application/json"
$sessionId = $sessionResponse.id
Write-Host "Session ID: $sessionId" -ForegroundColor Cyan

Write-Host "3. INITIALIZE GAME" -ForegroundColor Green
$gameResponse = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/init-game" -Method POST
$gameId = $gameResponse.id
Write-Host "Game ID: $gameId" -ForegroundColor Cyan
Write-Host "Board: '$($gameResponse.board)'" -ForegroundColor Cyan

Write-Host "4. MAKE FIRST MOVE (Position 0)" -ForegroundColor Green
$moveBody = @{
    playerId = $playerId
    position = 0
} | ConvertTo-Json

try {
    $moveResponse = Invoke-RestMethod -Uri "$baseUrl/api/games/$gameId/moves" -Method POST -Body $moveBody -ContentType "application/json"
    Write-Host "MOVE SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Board after move: '$($moveResponse.board)'" -ForegroundColor Cyan
} catch {
    Write-Host "MOVE FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "TEST COMPLETE!" -ForegroundColor Green 