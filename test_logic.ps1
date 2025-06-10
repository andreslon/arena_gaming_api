Write-Host "=== Testing Game Logic ===" -ForegroundColor Green

$baseUrl = "https://arenagaming-api.neolabs.com.co/api"

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/test/game-logic" -Method GET
    
    if ($response.Success) {
        Write-Host "Game Logic Test PASSED!" -ForegroundColor Green
        
        Write-Host "`nInitial State:" -ForegroundColor Yellow
        Write-Host "  Board: '$($response.InitialState.Board)'" -ForegroundColor Cyan
        Write-Host "  Board Length: $($response.InitialState.BoardLength)" -ForegroundColor Cyan
        Write-Host "  Status: $($response.InitialState.Status)" -ForegroundColor Cyan
        Write-Host "  Current Player: $($response.InitialState.CurrentPlayer)" -ForegroundColor Cyan
        
        Write-Host "`nAfter First Move:" -ForegroundColor Yellow
        Write-Host "  Board: '$($response.AfterFirstMove.Board)'" -ForegroundColor Cyan
        Write-Host "  Board Length: $($response.AfterFirstMove.BoardLength)" -ForegroundColor Cyan
        Write-Host "  Status: $($response.AfterFirstMove.Status)" -ForegroundColor Cyan
        Write-Host "  Current Player: $($response.AfterFirstMove.CurrentPlayer)" -ForegroundColor Cyan
        Write-Host "  Position 0 Value: '$($response.AfterFirstMove.Position0Value)'" -ForegroundColor Cyan
        Write-Host "  Position 0 ASCII: $($response.AfterFirstMove.Position0ASCII)" -ForegroundColor Cyan
        
        Write-Host "`nDuplicate Move Test:" -ForegroundColor Yellow
        Write-Host "  Error (Expected): $($response.DuplicateError)" -ForegroundColor Cyan
        
        Write-Host "`nAfter Second Move:" -ForegroundColor Yellow
        Write-Host "  Board: '$($response.AfterSecondMove.Board)'" -ForegroundColor Cyan
        Write-Host "  Board Length: $($response.AfterSecondMove.BoardLength)" -ForegroundColor Cyan
        Write-Host "  Status: $($response.AfterSecondMove.Status)" -ForegroundColor Cyan
        Write-Host "  Current Player: $($response.AfterSecondMove.CurrentPlayer)" -ForegroundColor Cyan
        
    } else {
        Write-Host "Game Logic Test FAILED!" -ForegroundColor Red
        Write-Host "Error: $($response.Error)" -ForegroundColor Red
        Write-Host "Stack Trace: $($response.StackTrace)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Test Endpoint Failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Logic Test Completed ===" -ForegroundColor Green 