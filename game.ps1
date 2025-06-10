Write-Host "HUMAN vs AI TIC-TAC-TOE" -ForegroundColor Red

taskkill /f /im ArenaGaming.Api.exe 2>$null
dotnet build --verbosity quiet
Start-Process powershell -ArgumentList "-Command", "cd '$PWD'; dotnet run --project ArenaGaming.Api" -WindowStyle Minimized
Start-Sleep 6

$board = @(' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ')
$turn = 1
$gameOver = $false
$sessionId = $null
$gameId = $null
$playerId = $null

function Show-Board {
    Write-Host ""
    Write-Host "  $($board[0]) $($board[1]) $($board[2])" -ForegroundColor White
    Write-Host "  $($board[3]) $($board[4]) $($board[5])" -ForegroundColor White
    Write-Host "  $($board[6]) $($board[7]) $($board[8])" -ForegroundColor White
    Write-Host ""
    Write-Host "  0 1 2" -ForegroundColor Gray
    Write-Host "  3 4 5" -ForegroundColor Gray
    Write-Host "  6 7 8" -ForegroundColor Gray
    Write-Host ""
}

function Check-Winner {
    $patterns = @(@(0,1,2), @(3,4,5), @(6,7,8), @(0,3,6), @(1,4,7), @(2,5,8), @(0,4,8), @(2,4,6))
    
    foreach ($pattern in $patterns) {
        if ($board[$pattern[0]] -ne ' ' -and $board[$pattern[0]] -eq $board[$pattern[1]] -and $board[$pattern[1]] -eq $board[$pattern[2]]) {
            return $board[$pattern[0]]
        }
    }
    
    $empty = 0
    foreach ($cell in $board) { if ($cell -eq ' ') { $empty++ } }
    if ($empty -eq 0) { return "TIE" }
    
    return $null
}

function Get-Available {
    $available = @()
    for ($i = 0; $i -lt 9; $i++) { if ($board[$i] -eq ' ') { $available += $i } }
    return $available
}

function Human-Move {
    $available = Get-Available
    if ($available.Count -eq 0) { return -1 }
    
    $strategy = Get-Random -Max 3
    
    if ($strategy -eq 0) {
        return $available[$(Get-Random -Max $available.Count)]
    } elseif ($strategy -eq 1 -and $available -contains 4) {
        return 4
    } else {
        $corners = @()
        foreach ($corner in @(0,2,6,8)) {
            if ($available -contains $corner) { $corners += $corner }
        }
        if ($corners.Count -gt 0) { 
            return $corners[$(Get-Random -Max $corners.Count)]
        }
        return $available[$(Get-Random -Max $available.Count)]
    }
}

Write-Host "STARTING GAME!" -ForegroundColor Green
Show-Board

while (-not $gameOver -and $turn -le 9) {
    Write-Host "=== TURN $turn ===" -ForegroundColor Yellow
    
    if ($turn % 2 -eq 1) {
        Write-Host "HUMAN TURN (X)" -ForegroundColor Blue
        
        # Create session and game on first human turn
        if (-not $sessionId) {
            Write-Host "Creating game session..." -ForegroundColor Yellow
            $playerId = [guid]::NewGuid()
            $createBody = @{ playerId = $playerId } | ConvertTo-Json
            $createResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/sessions" -Method POST -Body $createBody -ContentType "application/json"
            $sessionId = $createResponse.id
            Write-Host "Session created: $sessionId" -ForegroundColor Green
            
            # Initialize the game
            Write-Host "Initializing game..." -ForegroundColor Yellow
            $initResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/sessions/$sessionId/init-game" -Method POST -ContentType "application/json"
            $gameId = $initResponse.data.id
            Write-Host "Game initialized: $gameId" -ForegroundColor Green
        }
        
        $humanMove = Human-Move
        if ($humanMove -eq -1) { break }
        Write-Host "Human plays $humanMove" -ForegroundColor Blue
        $board[$humanMove] = 'X'
        
        # Make the move via API
        try {
            $moveBody = @{ 
                playerId = $playerId
                position = $humanMove
            } | ConvertTo-Json
            $moveResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/games/$gameId/moves" -Method POST -Body $moveBody -ContentType "application/json"
            Write-Host "Human move registered via API" -ForegroundColor Green
        } catch {
            Write-Host "Failed to register human move via API: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "AI TURN (O)" -ForegroundColor Red
        try {
            # Use the new AI suggest-move endpoint
            Write-Host "Calling REAL AI API..." -ForegroundColor Yellow
            $boardString = -join $board
            $body = @{
                board = $boardString
                currentPlayer = "O"
            } | ConvertTo-Json
            
            $aiResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/ai/suggest-move" -Method POST -Body $body -ContentType "application/json"
            $aiMove = $aiResponse.position
            
            if ($board[$aiMove] -ne ' ') {
                Write-Host "AI move invalid! Using backup..." -ForegroundColor Yellow
                $available = Get-Available
                $aiMove = $available[0]
            }
            
            Write-Host "REAL AI plays $aiMove" -ForegroundColor Red
            $board[$aiMove] = 'O'
        } catch {
            Write-Host "REAL API error: $($_.Exception.Message)" -ForegroundColor Red
            $available = Get-Available
            if ($available.Count -gt 0) {
                $aiMove = $available[0]
                $board[$aiMove] = 'O'
                Write-Host "AI fallback: $aiMove" -ForegroundColor Yellow
            } else { break }
        }
    }
    
    Show-Board
    
    $winner = Check-Winner
    if ($winner) {
        $gameOver = $true
        break
    }
    
    $turn++
    Start-Sleep 1
}

Write-Host "GAME OVER!" -ForegroundColor Magenta

if ($winner -eq 'X') {
    Write-Host "HUMAN WINS!" -ForegroundColor Green
} elseif ($winner -eq 'O') {
    Write-Host "AI WINS!" -ForegroundColor Red
} elseif ($winner -eq "TIE") {
    Write-Host "TIE GAME!" -ForegroundColor Yellow
} else {
    Write-Host "INCOMPLETE" -ForegroundColor Gray
}

Show-Board
taskkill /f /im ArenaGaming.Api.exe 2>$null 