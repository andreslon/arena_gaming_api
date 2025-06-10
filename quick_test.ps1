Write-Host "=== Quick API Test ===" -ForegroundColor Green

$baseUrl = "https://arenagaming-api.neolabs.com.co/api"
$playerId = "12345678-1234-1234-1234-123456789abc"

Write-Host "`nTesting Game Creation..." -ForegroundColor Yellow

try {
    $gameBody = @{
        playerId = $playerId
    }
    
    $gameResponse = Invoke-RestMethod -Uri "$baseUrl/games" -Method POST -Body ($gameBody | ConvertTo-Json) -ContentType "application/json"
    
    Write-Host "Game Created Successfully!" -ForegroundColor Green
    Write-Host "Game ID: $($gameResponse.id)" -ForegroundColor Cyan
    Write-Host "Initial Board: '$($gameResponse.board)'" -ForegroundColor Cyan
    Write-Host "Current Player: $($gameResponse.currentPlayerSymbol)" -ForegroundColor Cyan
    
    $gameId = $gameResponse.id
    
    Write-Host "`nTesting First Move (Position 0)..." -ForegroundColor Yellow
    
    $moveBody = @{
        playerId = $playerId
        position = 0
    }
    
    $moveResponse = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body ($moveBody | ConvertTo-Json) -ContentType "application/json"
    
    Write-Host "Move Successful!" -ForegroundColor Green
    Write-Host "Updated Board: '$($moveResponse.board)'" -ForegroundColor Cyan
    Write-Host "Current Player: $($moveResponse.currentPlayerSymbol)" -ForegroundColor Cyan
    
    Write-Host "`nTesting Duplicate Move (Position 0 again)..." -ForegroundColor Yellow
    
    try {
        $duplicateResponse = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body ($moveBody | ConvertTo-Json) -ContentType "application/json"
        Write-Host "ERROR: Duplicate move should have failed!" -ForegroundColor Red
    }
    catch {
        Write-Host "Duplicate move correctly rejected!" -ForegroundColor Green
        $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "Error: $($errorResponse.message)" -ForegroundColor Yellow
    }
    
    Write-Host "`nTesting Second Move (Position 1)..." -ForegroundColor Yellow
    
    $moveBody2 = @{
        playerId = $playerId
        position = 1
    }
    
    $moveResponse2 = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body ($moveBody2 | ConvertTo-Json) -ContentType "application/json"
    
    Write-Host "Second Move Successful!" -ForegroundColor Green
    Write-Host "Final Board: '$($moveResponse2.board)'" -ForegroundColor Cyan
    Write-Host "Current Player: $($moveResponse2.currentPlayerSymbol)" -ForegroundColor Cyan
    
}
catch {
    Write-Host "Test Failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "API Error: $($errorResponse.message)" -ForegroundColor Yellow
        Write-Host "Details: $($errorResponse.details)" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Test Completed ===" -ForegroundColor Green 