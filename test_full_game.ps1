Write-Host "🎮 TESTING FULL GAME - HUMAN vs AI" -ForegroundColor Green

# Kill any running API process
taskkill /f /im ArenaGaming.Api.exe 2>$null
Get-Process | Where-Object {$_.ProcessName -like "*ArenaGaming*"} | Stop-Process -Force 2>$null

Write-Host "Building..." -ForegroundColor Yellow
dotnet build --verbosity quiet

Write-Host "Starting API with detailed logging..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-Command", "cd '$PWD'; $env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project ArenaGaming.Api" -WindowStyle Minimized

Start-Sleep 6

Write-Host "`n🎯 SIMULATING FULL GAME" -ForegroundColor Cyan

# Test multiple scenarios to see if AI varies
$scenarios = @(
    @{ name = "Empty board"; board = "         "; player = "O" },
    @{ name = "X in corner"; board = "X        "; player = "O" },
    @{ name = "X in center"; board = "    X    "; player = "O" },
    @{ name = "Random game 1"; board = "X O      "; player = "X" },
    @{ name = "Random game 2"; board = " XO      "; player = "X" },
    @{ name = "Block situation"; board = "XX       "; player = "O" },
    @{ name = "Win situation"; board = "OO       "; player = "O" },
    @{ name = "Complex board"; board = "XOX O    "; player = "X" }
)

$results = @()

foreach ($scenario in $scenarios) {
    Write-Host "`n🔹 $($scenario.name):" -ForegroundColor White
    Write-Host "   Board: '$($scenario.board)'" -ForegroundColor Gray
    Write-Host "   Player: $($scenario.player)" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body "{`"board`": `"$($scenario.board)`", `"currentPlayer`": `"$($scenario.player)`"}" -ContentType "application/json"
        Write-Host "   AI Move: $($response.aiSuggestedMove)" -ForegroundColor Yellow
        
        $results += @{
            scenario = $scenario.name
            board = $scenario.board
            player = $scenario.player
            aiMove = $response.aiSuggestedMove
        }
    } catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep 1
}

Write-Host "`n📊 RESULTS ANALYSIS:" -ForegroundColor Green

# Check if AI is making different moves
$uniqueMoves = $results | ForEach-Object { $_.aiMove } | Sort-Object -Unique
Write-Host "Unique moves made: $($uniqueMoves -join ', ')" -ForegroundColor White

# Check for patterns
$centerMoves = ($results | Where-Object { $_.aiMove -eq 4 }).Count
$cornerMoves = ($results | Where-Object { $_.aiMove -in @(0,2,6,8) }).Count
$edgeMoves = ($results | Where-Object { $_.aiMove -in @(1,3,5,7) }).Count

Write-Host "Center moves (4): $centerMoves" -ForegroundColor Cyan
Write-Host "Corner moves (0,2,6,8): $cornerMoves" -ForegroundColor Cyan  
Write-Host "Edge moves (1,3,5,7): $edgeMoves" -ForegroundColor Cyan

# Test the same scenario multiple times to see if it varies
Write-Host "`n🔄 TESTING SAME SCENARIO 5 TIMES:" -ForegroundColor Yellow
$sameScenarioResults = @()

for ($i = 1; $i -le 5; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body '{"board": "    X    ", "currentPlayer": "O"}' -ContentType "application/json"
        $sameScenarioResults += $response.aiSuggestedMove
        Write-Host "   Attempt $i: $($response.aiSuggestedMove)" -ForegroundColor White
    } catch {
        Write-Host "   Attempt $i: ERROR" -ForegroundColor Red
    }
    Start-Sleep 0.5
}

$uniqueSame = $sameScenarioResults | Sort-Object -Unique
if ($uniqueSame.Count -eq 1) {
    Write-Host "`n❌ PROBLEM CONFIRMED: AI always returns $($uniqueSame[0]) for same scenario" -ForegroundColor Red
    Write-Host "This indicates Gemini is NOT being used properly!" -ForegroundColor Red
} else {
    Write-Host "`n✅ AI varies moves for same scenario: $($uniqueSame -join ', ')" -ForegroundColor Green
}

# Now test a complete game simulation
Write-Host "`n🎮 SIMULATING COMPLETE GAME:" -ForegroundColor Magenta

$gameBoard = "         "  # Empty board
$currentPlayer = "X"  # Human starts
$moves = @()

for ($turn = 1; $turn -le 9; $turn++) {
    Write-Host "`nTurn $turn - Player $currentPlayer" -ForegroundColor White
    Write-Host "Board: '$gameBoard'" -ForegroundColor Gray
    
    if ($currentPlayer -eq "X") {
        # Human move (simulate)
        $availablePositions = @()
        for ($i = 0; $i -lt 9; $i++) {
            if ($gameBoard[$i] -eq ' ') { $availablePositions += $i }
        }
        
        if ($availablePositions.Count -eq 0) { break }
        
        $humanMove = $availablePositions | Get-Random
        $gameBoard = $gameBoard.Remove($humanMove, 1).Insert($humanMove, "X")
        Write-Host "Human plays position $humanMove" -ForegroundColor Blue
                 $moves += "Turn $turn" + ": Human X -> $humanMove"
        $currentPlayer = "O"
    } else {
        # AI move
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:5000/api/testai/simple" -Method POST -Body "{`"board`": `"$gameBoard`", `"currentPlayer`": `"O`"}" -ContentType "application/json"
            $aiMove = $response.aiSuggestedMove
            
            if ($gameBoard[$aiMove] -ne ' ') {
                Write-Host "AI tried invalid move $aiMove!" -ForegroundColor Red
                break
            }
            
            $gameBoard = $gameBoard.Remove($aiMove, 1).Insert($aiMove, "O")
            Write-Host "AI plays position $aiMove" -ForegroundColor Yellow
            $moves += "Turn $turn" + ": AI O -> $aiMove"
            $currentPlayer = "X"
        } catch {
            Write-Host "AI move failed: $($_.Exception.Message)" -ForegroundColor Red
            break
        }
    }
    
    # Check win condition (simplified)
    $winPatterns = @(
        @(0,1,2), @(3,4,5), @(6,7,8),  # rows
        @(0,3,6), @(1,4,7), @(2,5,8),  # cols
        @(0,4,8), @(2,4,6)              # diagonals
    )
    
    $winner = $null
    foreach ($pattern in $winPatterns) {
        if ($gameBoard[$pattern[0]] -ne ' ' -and 
            $gameBoard[$pattern[0]] -eq $gameBoard[$pattern[1]] -and 
            $gameBoard[$pattern[1]] -eq $gameBoard[$pattern[2]]) {
            $winner = $gameBoard[$pattern[0]]
            break
        }
    }
    
    if ($winner) {
        Write-Host "`n🏆 GAME OVER! Winner: $winner" -ForegroundColor Green
        break
    }
}

Write-Host "`n📝 GAME MOVES:" -ForegroundColor Cyan
$moves | ForEach-Object { Write-Host "   $_" -ForegroundColor White }

Write-Host "`n🏁 Final board:" -ForegroundColor Green
Write-Host "   $($gameBoard[0])|$($gameBoard[1])|$($gameBoard[2])" -ForegroundColor White
Write-Host "   -----" -ForegroundColor White
Write-Host "   $($gameBoard[3])|$($gameBoard[4])|$($gameBoard[5])" -ForegroundColor White
Write-Host "   -----" -ForegroundColor White
Write-Host "   $($gameBoard[6])|$($gameBoard[7])|$($gameBoard[8])" -ForegroundColor White

# Kill API
taskkill /f /im ArenaGaming.Api.exe 2>$null 