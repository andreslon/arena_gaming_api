Write-Host "TESTING BASIC TIC-TAC-TOE LOGIC (No Redis required)" -ForegroundColor Red

$baseUrl = "http://localhost:5000"

Write-Host "1. TEST CONTROLLER - CREATE GAME" -ForegroundColor Green
try {
    $testResponse = Invoke-RestMethod -Uri "$baseUrl/api/test/create-game" -Method POST
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Game ID: $($testResponse.id)" -ForegroundColor Cyan
    Write-Host "Board: '$($testResponse.board)'" -ForegroundColor Cyan
    Write-Host "Board Length: $($testResponse.board.Length)" -ForegroundColor Yellow
    Write-Host "Status: $($testResponse.status)" -ForegroundColor Cyan
    Write-Host "Current Player: $($testResponse.currentPlayerSymbol)" -ForegroundColor Cyan
} catch {
    Write-Host "FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "2. TEST CONTROLLER - MAKE MOVE" -ForegroundColor Green
try {
    $moveResponse = Invoke-RestMethod -Uri "$baseUrl/api/test/make-move" -Method POST
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Board after move: '$($moveResponse.board)'" -ForegroundColor Cyan
    Write-Host "Current Player: $($moveResponse.currentPlayerSymbol)" -ForegroundColor Cyan
    Write-Host "Status: $($moveResponse.status)" -ForegroundColor Cyan
} catch {
    Write-Host "FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "BASIC TEST COMPLETE!" -ForegroundColor Green 