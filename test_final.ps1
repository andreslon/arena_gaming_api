Write-Host "🔥 FINAL TEST - GEMINI vs FALLBACK" -ForegroundColor Red

taskkill /f /im ArenaGaming.Api.exe 2>$null
dotnet build --verbosity quiet

Write-Host "Starting API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-Command", "cd '$PWD'; dotnet run --project ArenaGaming.Api" -WindowStyle Minimized

Start-Sleep 5

Write-Host "`nMaking AI call..." -ForegroundColor Cyan
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body '{"board": "         ", "currentPlayer": "O"}' -ContentType "application/json"

Write-Host "AI Response: $($result.aiSuggestedMove)" -ForegroundColor Yellow

taskkill /f /im ArenaGaming.Api.exe 2>$null

Write-Host "Check console output above" -ForegroundColor Green 