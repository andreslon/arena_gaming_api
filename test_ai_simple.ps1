Write-Host "🤖 TESTING GEMINI AI DIRECTLY" -ForegroundColor Green

# Kill any running API process
taskkill /f /im ArenaGaming.Api.exe 2>$null
Get-Process | Where-Object {$_.ProcessName -like "*ArenaGaming*"} | Stop-Process -Force 2>$null

Write-Host "Building..." -ForegroundColor Yellow
dotnet build --verbosity quiet

Write-Host "Starting API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-Command", "cd '$PWD'; dotnet run --project ArenaGaming.Api" -WindowStyle Hidden

Start-Sleep 5

Write-Host "Testing different scenarios:" -ForegroundColor Cyan

# Test 1: Empty board
Write-Host "`n1. Empty board:" -ForegroundColor White
$response1 = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body '{"board": "         ", "currentPlayer": "O"}' -ContentType "application/json"
Write-Host "   AI Move: $($response1.aiSuggestedMove)" -ForegroundColor Yellow

# Test 2: Center taken
Write-Host "`n2. Center taken by X:" -ForegroundColor White
$response2 = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body '{"board": "    X    ", "currentPlayer": "O"}' -ContentType "application/json"
Write-Host "   AI Move: $($response2.aiSuggestedMove)" -ForegroundColor Yellow

# Test 3: Should block
Write-Host "`n3. Should block winning move:" -ForegroundColor White
$response3 = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body '{"board": "XX       ", "currentPlayer": "O"}' -ContentType "application/json"
Write-Host "   AI Move: $($response3.aiSuggestedMove) (should be 2)" -ForegroundColor Yellow

# Test 4: Should win
Write-Host "`n4. Should win:" -ForegroundColor White
$response4 = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body '{"board": "OO       ", "currentPlayer": "O"}' -ContentType "application/json"
Write-Host "   AI Move: $($response4.aiSuggestedMove) (should be 2)" -ForegroundColor Yellow

Write-Host "`n🎯 SUMMARY:" -ForegroundColor Green
Write-Host "   Test 1 (empty): $($response1.aiSuggestedMove)"
Write-Host "   Test 2 (center taken): $($response2.aiSuggestedMove)"
Write-Host "   Test 3 (should block): $($response3.aiSuggestedMove)"
Write-Host "   Test 4 (should win): $($response4.aiSuggestedMove)"

if ($response1.aiSuggestedMove -ne $response2.aiSuggestedMove -or 
    $response3.aiSuggestedMove -eq 2 -or 
    $response4.aiSuggestedMove -eq 2) {
    Write-Host "`n✅ AI is working and varying moves!" -ForegroundColor Green
} else {
    Write-Host "`n❌ AI is still broken - always same move" -ForegroundColor Red
}

# Kill API
taskkill /f /im ArenaGaming.Api.exe 2>$null 