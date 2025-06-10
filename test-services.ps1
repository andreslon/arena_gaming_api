#!/usr/bin/env pwsh

Write-Host "🧪 Testing Arena Gaming API Services" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Configuration
$baseUrl = "http://localhost:5000"  # Change this to your actual API URL when deployed
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# Function to make HTTP requests with error handling
function Invoke-ApiTest {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [string]$ServiceName,
        [hashtable]$Body = $null
    )
    
    Write-Host "`n🔍 Testing $ServiceName..." -ForegroundColor Yellow
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $headers
            TimeoutSec = 30
        }
        
        if ($Body -and $Method -eq "POST") {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-RestMethod @params
        
        Write-Host "✅ $ServiceName - SUCCESS" -ForegroundColor Green
        Write-Host "Response:" -ForegroundColor Gray
        $response | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor Gray
        
        return @{ Success = $true; Response = $response }
    }
    catch {
        Write-Host "❌ $ServiceName - FAILED" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
        
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Test Results Storage
$testResults = @{}

Write-Host "`n📊 Starting Health Checks..." -ForegroundColor Magenta

# 1. General Health Check
$result = Invoke-ApiTest -Url "$baseUrl/api/health" -ServiceName "General Health"
$testResults["GeneralHealth"] = $result

# 2. PostgreSQL Health Check
$result = Invoke-ApiTest -Url "$baseUrl/api/health/postgresql" -ServiceName "PostgreSQL Health"
$testResults["PostgreSQLHealth"] = $result

# 3. Redis Health Check
$result = Invoke-ApiTest -Url "$baseUrl/api/health/redis" -ServiceName "Redis Health"
$testResults["RedisHealth"] = $result

# 4. Pulsar Health Check
$result = Invoke-ApiTest -Url "$baseUrl/api/health/pulsar" -ServiceName "Pulsar Health"
$testResults["PulsarHealth"] = $result

Write-Host "`n🔧 Starting Detailed Service Tests..." -ForegroundColor Magenta

# 5. PostgreSQL Detailed Test
$result = Invoke-ApiTest -Url "$baseUrl/api/health/postgresql/test" -Method "POST" -ServiceName "PostgreSQL Operations Test"
$testResults["PostgreSQLTest"] = $result

# 6. Redis Detailed Test
$result = Invoke-ApiTest -Url "$baseUrl/api/health/redis/test" -Method "POST" -ServiceName "Redis Operations Test"
$testResults["RedisTest"] = $result

# 7. Pulsar Detailed Test
$result = Invoke-ApiTest -Url "$baseUrl/api/health/pulsar/test" -Method "POST" -ServiceName "Pulsar Operations Test"
$testResults["PulsarTest"] = $result

# 8. Gemini AI Direct Test
$result = Invoke-ApiTest -Url "$baseUrl/api/health/gemini/test" -Method "POST" -ServiceName "Gemini AI Direct Test"
$testResults["GeminiDirectTest"] = $result

Write-Host "`n🎮 Testing Game Logic (which uses Gemini AI)..." -ForegroundColor Magenta

# 9. Test Game Creation (which indirectly tests the system)
$createGameBody = @{
    PlayerId = [System.Guid]::NewGuid().ToString()
}
$result = Invoke-ApiTest -Url "$baseUrl/api/games" -Method "POST" -ServiceName "Game Creation" -Body $createGameBody
$testResults["GameCreation"] = $result

# If game creation succeeded, test AI move (which uses Gemini)
if ($result.Success -and $result.Response.Id) {
    $gameId = $result.Response.Id
    Write-Host "`n🤖 Testing AI Move (Gemini Integration)..." -ForegroundColor Magenta
    
    # Make a move to start the game
    $makeMoveBody = @{
        PlayerId = $createGameBody.PlayerId
        Position = 0  # Top-left corner
    }
    
    $moveResult = Invoke-ApiTest -Url "$baseUrl/api/games/$gameId/moves" -Method "POST" -ServiceName "Player Move" -Body $makeMoveBody
    $testResults["PlayerMove"] = $moveResult
    
    if ($moveResult.Success) {
        # Get the game state to see if AI made a counter-move
        Start-Sleep -Seconds 2  # Give time for AI to process
        $gameStateResult = Invoke-ApiTest -Url "$baseUrl/api/games/$gameId" -ServiceName "Game State Check (AI Move)"
        $testResults["AIMove"] = $gameStateResult
        
        if ($gameStateResult.Success) {
            $board = $gameStateResult.Response.Board
            $moveCount = ($board.ToCharArray() | Where-Object { $_ -ne ' ' }).Count
            
            if ($moveCount -gt 1) {
                Write-Host "✅ GEMINI AI - SUCCESS (AI made a move)" -ForegroundColor Green
            } else {
                Write-Host "⚠️  GEMINI AI - UNCLEAR (AI may not have moved yet)" -ForegroundColor Yellow
            }
        }
    }
}

Write-Host "`n📋 TEST SUMMARY" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan

$successCount = 0
$totalTests = $testResults.Count

foreach ($testName in $testResults.Keys) {
    $result = $testResults[$testName]
    if ($result.Success) {
        Write-Host "✅ $testName" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "❌ $testName" -ForegroundColor Red
    }
}

Write-Host "`n📊 Results: $successCount/$totalTests tests passed" -ForegroundColor $(if ($successCount -eq $totalTests) { "Green" } else { "Yellow" })

if ($successCount -eq $totalTests) {
    Write-Host "🎉 All services are working correctly!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Some services need attention. Check the failed tests above." -ForegroundColor Yellow
}

Write-Host "`n🔗 Tested Services:" -ForegroundColor Cyan
Write-Host "   • PostgreSQL (Health + Operations)" -ForegroundColor Gray
Write-Host "   • Redis (Health + Operations)" -ForegroundColor Gray  
Write-Host "   • Pulsar (Health + Operations)" -ForegroundColor Gray
Write-Host "   • Gemini AI (Direct Test + Game Logic)" -ForegroundColor Gray
Write-Host "   • General Application Health" -ForegroundColor Gray 