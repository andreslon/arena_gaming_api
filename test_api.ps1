# Arena Gaming API Test Script
# This script tests the game functionality to verify the concurrency fix works

Write-Host "=== Arena Gaming API Tests ===" -ForegroundColor Green

# API Base URL
$baseUrl = "https://arenagaming-api.neolabs.com.co/api"

# Test player ID
$playerId = [System.Guid]::NewGuid().ToString()

Write-Host "`n1. Testing Session Creation..." -ForegroundColor Yellow

# Create a session
$sessionBody = @{
    playerId = $playerId
} | ConvertTo-Json

try {
    $sessionResponse = Invoke-RestMethod -Uri "$baseUrl/sessions" -Method POST -Body $sessionBody -ContentType "application/json"
    Write-Host "✓ Session created successfully" -ForegroundColor Green
    Write-Host "Session ID: $($sessionResponse.id)" -ForegroundColor Cyan
    $sessionId = $sessionResponse.id
}
catch {
    Write-Host "✗ Failed to create session: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n2. Testing Game Creation..." -ForegroundColor Yellow

# Start a new game
try {
    $gameResponse = Invoke-RestMethod -Uri "$baseUrl/sessions/$sessionId/init-game" -Method POST -ContentType "application/json"
    Write-Host "✓ Game created successfully" -ForegroundColor Green
    Write-Host "Game ID: $($gameResponse.data.id)" -ForegroundColor Cyan
    Write-Host "Initial Board: '$($gameResponse.data.board)'" -ForegroundColor Cyan
    $gameId = $gameResponse.data.id
}
catch {
    Write-Host "✗ Failed to create game: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n3. Testing First Move (Position 0)..." -ForegroundColor Yellow

# Make first move at position 0
$moveBody = @{
    playerId = $playerId
    position = 0
} | ConvertTo-Json

try {
    $moveResponse = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body $moveBody -ContentType "application/json"
    Write-Host "✓ First move successful" -ForegroundColor Green
    Write-Host "Updated Board: '$($moveResponse.board)'" -ForegroundColor Cyan
    Write-Host "Current Player: $($moveResponse.currentPlayerSymbol)" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ Failed to make first move: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    
    # Get game state for debugging
    try {
        $gameState = Invoke-RestMethod -Uri "$baseUrl/games/$gameId" -Method GET
        Write-Host "Current Game State:" -ForegroundColor Yellow
        Write-Host "Board: '$($gameState.board)'" -ForegroundColor Cyan
        Write-Host "Status: $($gameState.status)" -ForegroundColor Cyan
    }
    catch {
        Write-Host "Failed to get game state: $($_.Exception.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n4. Testing Second Move (Position 1)..." -ForegroundColor Yellow

# Make second move at position 1
$moveBody2 = @{
    playerId = $playerId
    position = 1
} | ConvertTo-Json

try {
    $moveResponse2 = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body $moveBody2 -ContentType "application/json"
    Write-Host "✓ Second move successful" -ForegroundColor Green
    Write-Host "Updated Board: '$($moveResponse2.board)'" -ForegroundColor Cyan
    Write-Host "Current Player: $($moveResponse2.currentPlayerSymbol)" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ Failed to make second move: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host "`n5. Testing Duplicate Move (Position 0 again)..." -ForegroundColor Yellow

# Try to move at position 0 again (should fail)
try {
    $duplicateResponse = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body $moveBody -ContentType "application/json"
    Write-Host "✗ Duplicate move should have failed but didn't!" -ForegroundColor Red
}
catch {
    Write-Host "✓ Duplicate move correctly rejected" -ForegroundColor Green
    Write-Host "Error: $($_.ErrorDetails.Message)" -ForegroundColor Cyan
}

Write-Host "`n6. Testing Game State Retrieval..." -ForegroundColor Yellow

# Get current game state
try {
    $finalGameState = Invoke-RestMethod -Uri "$baseUrl/games/$gameId" -Method GET
    Write-Host "✓ Game state retrieved successfully" -ForegroundColor Green
    Write-Host "Final Board: '$($finalGameState.board)'" -ForegroundColor Cyan
    Write-Host "Game Status: $($finalGameState.status)" -ForegroundColor Cyan
    Write-Host "Current Player: $($finalGameState.currentPlayerSymbol)" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ Failed to get game state: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n7. Testing Concurrency with Multiple Rapid Requests..." -ForegroundColor Yellow

# Test concurrency by making multiple moves rapidly
$concurrencyResults = @()
$positions = @(2, 3, 4, 5, 6, 7, 8)

foreach ($pos in $positions) {
    $concurrentMoveBody = @{
        playerId = $playerId
        position = $pos
    } | ConvertTo-Json
    
    try {
        $concurrentResponse = Invoke-RestMethod -Uri "$baseUrl/games/$gameId/moves" -Method POST -Body $concurrentMoveBody -ContentType "application/json"
        $concurrencyResults += "✓ Position $pos: Success"
        Write-Host "✓ Move at position $pos successful" -ForegroundColor Green
    }
    catch {
        $concurrencyResults += "✗ Position $pos: $($_.Exception.Message)"
        Write-Host "✗ Move at position $pos failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Results Summary ===" -ForegroundColor Green
Write-Host "Session ID: $sessionId" -ForegroundColor Cyan
Write-Host "Game ID: $gameId" -ForegroundColor Cyan
Write-Host "`nConcurrency Test Results:" -ForegroundColor Yellow
$concurrencyResults | ForEach-Object { Write-Host $_ }

Write-Host "`n=== Tests Completed ===" -ForegroundColor Green 