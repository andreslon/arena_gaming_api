Write-Host "=== Debug API Test ===" -ForegroundColor Green

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
    Write-Host "Board Length: $($gameResponse.board.Length)" -ForegroundColor Cyan
    Write-Host "Current Player: $($gameResponse.currentPlayerSymbol)" -ForegroundColor Cyan
    Write-Host "Player ID: $($gameResponse.playerId)" -ForegroundColor Cyan
    
    $gameId = $gameResponse.id
    
    Write-Host "`nTesting Game State Retrieval..." -ForegroundColor Yellow
    
    $gameState = Invoke-RestMethod -Uri "$baseUrl/games/$gameId" -Method GET
    Write-Host "Retrieved Board: '$($gameState.board)'" -ForegroundColor Cyan
    Write-Host "Retrieved Board Length: $($gameState.board.Length)" -ForegroundColor Cyan
    Write-Host "Game Status: $($gameState.status)" -ForegroundColor Cyan
    
    Write-Host "`nTesting First Move (Position 0)..." -ForegroundColor Yellow
    
    $moveBody = @{
        playerId = $playerId
        position = 0
    }
    
    Write-Host "Move Request Body: $($moveBody | ConvertTo-Json)" -ForegroundColor Cyan
    
    try {
        $moveResponse = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body ($moveBody | ConvertTo-Json) -ContentType "application/json"
        
        Write-Host "Move Successful!" -ForegroundColor Green
        Write-Host "Updated Board: '$($moveResponse.board)'" -ForegroundColor Cyan
        Write-Host "Current Player: $($moveResponse.currentPlayerSymbol)" -ForegroundColor Cyan
    }
    catch {
        Write-Host "Move Failed!" -ForegroundColor Red
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.ErrorDetails.Message) {
            Write-Host "Raw Error Response: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
            try {
                $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
                Write-Host "Parsed Error: $($errorResponse.message)" -ForegroundColor Yellow
                if ($errorResponse.details) {
                    Write-Host "Details: $($errorResponse.details)" -ForegroundColor Yellow
                }
            }
            catch {
                Write-Host "Could not parse error response as JSON" -ForegroundColor Red
            }
        }
    }
    
}
catch {
    Write-Host "Test Failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Error Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Debug Test Completed ===" -ForegroundColor Green 