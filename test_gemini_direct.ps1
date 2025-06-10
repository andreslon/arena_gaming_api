Write-Host "🧠 TESTING GEMINI API DIRECTLY" -ForegroundColor Green

$apiKey = "AIzaSyAxMlZ80j89_vjoqXAu__1NmYC4yi2bzmg"
$url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=$apiKey"

$scenarios = @(
    @{ name = "Empty board"; board = "         "; player = "O" },
    @{ name = "Center taken"; board = "    X    "; player = "O" },
    @{ name = "Two X's"; board = "XX       "; player = "O" }
)

foreach ($scenario in $scenarios) {
    Write-Host "`n🔹 Testing: $($scenario.name)" -ForegroundColor Cyan
    Write-Host "   Board: '$($scenario.board)'" -ForegroundColor Gray
    Write-Host "   Player: $($scenario.player)" -ForegroundColor Gray
    
    $prompt = @"
Tic-tac-toe board (9 positions 0-8):
$($scenario.board[0])|$($scenario.board[1])|$($scenario.board[2])
$($scenario.board[3])|$($scenario.board[4])|$($scenario.board[5])
$($scenario.board[6])|$($scenario.board[7])|$($scenario.board[8])

You are '$($scenario.player)'. Choose the best position (0-8).
Respond with ONLY the number.
"@

    $requestBody = @{
        contents = @(
            @{
                parts = @(
                    @{ text = $prompt }
                )
            }
        )
        generationConfig = @{
            temperature = 0.5
            maxOutputTokens = 10
        }
    } | ConvertTo-Json -Depth 10

    try {
        Write-Host "   Calling Gemini..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $url -Method POST -Body $requestBody -ContentType "application/json"
        
        if ($response.candidates -and $response.candidates.Count -gt 0) {
            $text = $response.candidates[0].content.parts[0].text.Trim()
            Write-Host "   Gemini raw response: '$text'" -ForegroundColor White
            
            if ($text -match '\d') {
                $move = [int][char]$Matches[0] - [int][char]'0'
                Write-Host "   🎯 Extracted move: $move" -ForegroundColor Green
            } else {
                Write-Host "   ❌ No valid move found" -ForegroundColor Red
            }
        } else {
            Write-Host "   ❌ No candidates in response" -ForegroundColor Red
        }
    } catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep 2
}

Write-Host "`n🔄 TESTING SAME SCENARIO 3 TIMES:" -ForegroundColor Magenta

$testBoard = "    X    "
$testPlayer = "O"

for ($i = 1; $i -le 3; $i++) {
    Write-Host "`n   Test $i:" -ForegroundColor White
    
    $prompt = @"
Tic-tac-toe board (9 positions 0-8):
$($testBoard[0])|$($testBoard[1])|$($testBoard[2])
$($testBoard[3])|$($testBoard[4])|$($testBoard[5])
$($testBoard[6])|$($testBoard[7])|$($testBoard[8])

You are '$testPlayer'. Choose the best position (0-8).
Respond with ONLY the number.
"@

    $requestBody = @{
        contents = @(
            @{
                parts = @(
                    @{ text = $prompt }
                )
            }
        )
        generationConfig = @{
                         temperature = 0.8
            maxOutputTokens = 10
            topK = 40
            topP = 0.95
        }
    } | ConvertTo-Json -Depth 10

    try {
        $response = Invoke-RestMethod -Uri $url -Method POST -Body $requestBody -ContentType "application/json"
        
        if ($response.candidates -and $response.candidates.Count -gt 0) {
            $text = $response.candidates[0].content.parts[0].text.Trim()
            
            if ($text -match '\d') {
                $move = [int][char]$Matches[0] - [int][char]'0'
                Write-Host "      Move: $move (from '$text')" -ForegroundColor Yellow
            }
        }
    } catch {
        Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep 1
}

Write-Host "`n📊 If all 3 tests return the same move, Gemini is being deterministic" -ForegroundColor Cyan
Write-Host "📊 If moves vary, then the problem is in our API code" -ForegroundColor Cyan 