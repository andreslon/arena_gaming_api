#!/bin/bash

# Arena Gaming API Test Script (Bash/Curl version)
# This script tests the game functionality to verify the concurrency fix works

echo "=== Arena Gaming API Tests ==="

# API Base URL
BASE_URL="https://arenagaming-api.neolabs.com.co/api"

# Generate a test player ID
PLAYER_ID=$(uuidgen)

echo
echo "1. Testing Session Creation..."

# Create a session
SESSION_RESPONSE=$(curl -s -X POST "$BASE_URL/sessions" \
  -H "Content-Type: application/json" \
  -d "{\"playerId\": \"$PLAYER_ID\"}")

if [ $? -eq 0 ]; then
    SESSION_ID=$(echo $SESSION_RESPONSE | jq -r '.id')
    echo "✓ Session created successfully"
    echo "Session ID: $SESSION_ID"
else
    echo "✗ Failed to create session"
    exit 1
fi

echo
echo "2. Testing Game Creation..."

# Start a new game
GAME_RESPONSE=$(curl -s -X POST "$BASE_URL/sessions/$SESSION_ID/init-game" \
  -H "Content-Type: application/json")

if [ $? -eq 0 ]; then
    GAME_ID=$(echo $GAME_RESPONSE | jq -r '.data.id')
    INITIAL_BOARD=$(echo $GAME_RESPONSE | jq -r '.data.board')
    echo "✓ Game created successfully"
    echo "Game ID: $GAME_ID"
    echo "Initial Board: '$INITIAL_BOARD'"
else
    echo "✗ Failed to create game"
    echo "Response: $GAME_RESPONSE"
    exit 1
fi

echo
echo "3. Testing First Move (Position 0)..."

# Make first move at position 0
MOVE_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
  -H "Content-Type: application/json" \
  -d "{\"playerId\": \"$PLAYER_ID\", \"position\": 0}")

if echo $MOVE_RESPONSE | jq -e '.board' > /dev/null 2>&1; then
    UPDATED_BOARD=$(echo $MOVE_RESPONSE | jq -r '.board')
    CURRENT_PLAYER=$(echo $MOVE_RESPONSE | jq -r '.currentPlayerSymbol')
    echo "✓ First move successful"
    echo "Updated Board: '$UPDATED_BOARD'"
    echo "Current Player: $CURRENT_PLAYER"
else
    echo "✗ Failed to make first move"
    echo "Response: $MOVE_RESPONSE"
    
    # Get game state for debugging
    GAME_STATE=$(curl -s -X GET "$BASE_URL/games/$GAME_ID")
    echo "Current Game State: $GAME_STATE"
    exit 1
fi

echo
echo "4. Testing Second Move (Position 1)..."

# Make second move at position 1
MOVE2_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
  -H "Content-Type: application/json" \
  -d "{\"playerId\": \"$PLAYER_ID\", \"position\": 1}")

if echo $MOVE2_RESPONSE | jq -e '.board' > /dev/null 2>&1; then
    UPDATED_BOARD2=$(echo $MOVE2_RESPONSE | jq -r '.board')
    CURRENT_PLAYER2=$(echo $MOVE2_RESPONSE | jq -r '.currentPlayerSymbol')
    echo "✓ Second move successful"
    echo "Updated Board: '$UPDATED_BOARD2'"
    echo "Current Player: $CURRENT_PLAYER2"
else
    echo "✗ Failed to make second move"
    echo "Response: $MOVE2_RESPONSE"
fi

echo
echo "5. Testing Duplicate Move (Position 0 again)..."

# Try to move at position 0 again (should fail)
DUPLICATE_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
  -H "Content-Type: application/json" \
  -d "{\"playerId\": \"$PLAYER_ID\", \"position\": 0}")

if echo $DUPLICATE_RESPONSE | jq -e '.error' > /dev/null 2>&1; then
    echo "✓ Duplicate move correctly rejected"
    echo "Error: $(echo $DUPLICATE_RESPONSE | jq -r '.message')"
else
    echo "✗ Duplicate move should have failed but didn't!"
    echo "Response: $DUPLICATE_RESPONSE"
fi

echo
echo "6. Testing Game State Retrieval..."

# Get current game state
FINAL_STATE=$(curl -s -X GET "$BASE_URL/games/$GAME_ID")

if echo $FINAL_STATE | jq -e '.board' > /dev/null 2>&1; then
    FINAL_BOARD=$(echo $FINAL_STATE | jq -r '.board')
    GAME_STATUS=$(echo $FINAL_STATE | jq -r '.status')
    FINAL_PLAYER=$(echo $FINAL_STATE | jq -r '.currentPlayerSymbol')
    echo "✓ Game state retrieved successfully"
    echo "Final Board: '$FINAL_BOARD'"
    echo "Game Status: $GAME_STATUS"
    echo "Current Player: $FINAL_PLAYER"
else
    echo "✗ Failed to get game state"
    echo "Response: $FINAL_STATE"
fi

echo
echo "7. Testing Concurrency with Multiple Rapid Requests..."

# Test concurrency by making multiple moves rapidly
POSITIONS=(2 3 4 5 6 7 8)
CONCURRENCY_RESULTS=()

for pos in "${POSITIONS[@]}"; do
    CONCURRENT_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
      -H "Content-Type: application/json" \
      -d "{\"playerId\": \"$PLAYER_ID\", \"position\": $pos}")
    
    if echo $CONCURRENT_RESPONSE | jq -e '.board' > /dev/null 2>&1; then
        echo "✓ Move at position $pos successful"
        CONCURRENCY_RESULTS+=("✓ Position $pos: Success")
    else
        ERROR_MSG=$(echo $CONCURRENT_RESPONSE | jq -r '.message // .error // "Unknown error"')
        echo "✗ Move at position $pos failed: $ERROR_MSG"
        CONCURRENCY_RESULTS+=("✗ Position $pos: $ERROR_MSG")
    fi
done

echo
echo "=== Test Results Summary ==="
echo "Session ID: $SESSION_ID"
echo "Game ID: $GAME_ID"
echo
echo "Concurrency Test Results:"
for result in "${CONCURRENCY_RESULTS[@]}"; do
    echo "$result"
done

echo
echo "=== Tests Completed ===" 