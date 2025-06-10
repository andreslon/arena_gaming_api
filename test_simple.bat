@echo off
echo === Arena Gaming API Tests ===

REM Generate a test player ID (simple UUID-like string)
set PLAYER_ID=12345678-1234-1234-1234-123456789abc

echo.
echo 1. Testing Session Creation...

REM Create a session
curl -X POST "https://arenagaming-api.neolabs.com.co/api/sessions" ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\": \"%PLAYER_ID%\"}" ^
  --output session_response.json

if exist session_response.json (
    echo Session created successfully
    type session_response.json
) else (
    echo Failed to create session
    exit /b 1
)

echo.
echo 2. Testing Direct Game Creation...

REM Try to create a game directly
curl -X POST "https://arenagaming-api.neolabs.com.co/api/games" ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\": \"%PLAYER_ID%\"}" ^
  --output game_response.json

if exist game_response.json (
    echo Game created successfully
    type game_response.json
) else (
    echo Failed to create game
    exit /b 1
)

echo.
echo 3. Testing First Move (Position 0)...

REM We need to extract game ID from the response, for now use a placeholder
REM In a real scenario, you'd parse the JSON to get the actual game ID
set GAME_ID=placeholder

curl -X POST "https://arenagaming-api.neolabs.com.co/api/games/%GAME_ID%/moves" ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\": \"%PLAYER_ID%\", \"position\": 0}" ^
  --output move_response.json

if exist move_response.json (
    echo Move response:
    type move_response.json
) else (
    echo Failed to make move
)

echo.
echo === Manual Testing Commands ===
echo Use these curl commands to test manually:
echo.
echo 1. Create Session:
echo curl -X POST "https://arenagaming-api.neolabs.com.co/api/sessions" -H "Content-Type: application/json" -d "{\"playerId\": \"%PLAYER_ID%\"}"
echo.
echo 2. Create Game:
echo curl -X POST "https://arenagaming-api.neolabs.com.co/api/games" -H "Content-Type: application/json" -d "{\"playerId\": \"%PLAYER_ID%\"}"
echo.
echo 3. Make Move (replace GAME_ID):
echo curl -X POST "https://arenagaming-api.neolabs.com.co/api/games/GAME_ID/moves" -H "Content-Type: application/json" -d "{\"playerId\": \"%PLAYER_ID%\", \"position\": 0}"
echo.
echo 4. Get Game State:
echo curl -X GET "https://arenagaming-api.neolabs.com.co/api/games/GAME_ID"

pause 