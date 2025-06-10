#!/bin/bash

# Arena Gaming API - Test completo de endpoints
# Usage: chmod +x test_endpoints.sh && ./test_endpoints.sh

BASE_URL="https://arenagaming-api.neolabs.com.co/api"
PLAYER_ID="550e8400-e29b-41d4-a716-446655440000"

echo "🚀 TESTING ARENA GAMING API ENDPOINTS"
echo "Base URL: $BASE_URL"
echo "Player ID: $PLAYER_ID"
echo "=================================================="

# 1. Health Check General
echo -e "\n1️⃣  HEALTH CHECK GENERAL"
curl -s -X GET "$BASE_URL/health" | jq '.' || echo "❌ Health check failed"

# 2. Test Game Logic
echo -e "\n2️⃣  TEST GAME LOGIC"
curl -s -X GET "$BASE_URL/test/game-logic" | jq '.' || echo "❌ Game logic test failed"

# 3. Health Checks Específicos
echo -e "\n3️⃣  HEALTH CHECK POSTGRESQL"
curl -s -X GET "$BASE_URL/health/postgresql" | jq '.' || echo "❌ PostgreSQL health check failed"

echo -e "\n4️⃣  HEALTH CHECK REDIS"
curl -s -X GET "$BASE_URL/health/redis" | jq '.' || echo "❌ Redis health check failed"

echo -e "\n5️⃣  HEALTH CHECK PULSAR"
curl -s -X GET "$BASE_URL/health/pulsar" | jq '.' || echo "❌ Pulsar health check failed"

# 6. Crear Sesión
echo -e "\n6️⃣  CREATING SESSION"
SESSION_RESPONSE=$(curl -s -X POST "$BASE_URL/sessions" \
  -H "Content-Type: application/json" \
  -d "{\"playerId\": \"$PLAYER_ID\"}")

echo "$SESSION_RESPONSE" | jq '.'

SESSION_ID=$(echo "$SESSION_RESPONSE" | jq -r '.id')
if [ "$SESSION_ID" != "null" ] && [ "$SESSION_ID" != "" ]; then
    echo "✅ Session created successfully: $SESSION_ID"
    
    # 7. Iniciar Juego
    echo -e "\n7️⃣  STARTING GAME"
    GAME_RESPONSE=$(curl -s -X POST "$BASE_URL/sessions/$SESSION_ID/init-game")
    echo "$GAME_RESPONSE" | jq '.'
    
    GAME_ID=$(echo "$GAME_RESPONSE" | jq -r '.data.id')
    if [ "$GAME_ID" != "null" ] && [ "$GAME_ID" != "" ]; then
        echo "✅ Game started successfully: $GAME_ID"
        
        # 8. Hacer primer movimiento
        echo -e "\n8️⃣  MAKING FIRST MOVE (position 0)"
        MOVE1_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
          -H "Content-Type: application/json" \
          -d "{\"playerId\": \"$PLAYER_ID\", \"position\": 0}")
        echo "$MOVE1_RESPONSE" | jq '.'
        
        # 9. Hacer segundo movimiento
        echo -e "\n9️⃣  MAKING SECOND MOVE (position 1)"
        MOVE2_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
          -H "Content-Type: application/json" \
          -d "{\"playerId\": \"$PLAYER_ID\", \"position\": 1}")
        echo "$MOVE2_RESPONSE" | jq '.'
        
        # 10. Intentar movimiento duplicado (debe fallar)
        echo -e "\n🔟 TESTING DUPLICATE MOVE (should fail)"
        DUPLICATE_RESPONSE=$(curl -s -X POST "$BASE_URL/games/$GAME_ID/moves" \
          -H "Content-Type: application/json" \
          -d "{\"playerId\": \"$PLAYER_ID\", \"position\": 0}")
        echo "$DUPLICATE_RESPONSE" | jq '.'
        
        # 11. Obtener estado del juego
        echo -e "\n1️⃣1️⃣ GETTING GAME STATE"
        GAME_STATE=$(curl -s -X GET "$BASE_URL/games/$GAME_ID")
        echo "$GAME_STATE" | jq '.'
        
        # 12. Hacer movimiento de IA
        echo -e "\n1️⃣2️⃣ MAKING AI MOVE"
        AI_MOVE_RESPONSE=$(curl -s -X POST "$BASE_URL/sessions/$SESSION_ID/ai-move")
        echo "$AI_MOVE_RESPONSE" | jq '.'
        
    else
        echo "❌ Failed to start game"
    fi
else
    echo "❌ Failed to create session"
fi

# 13. Test Notificaciones
echo -e "\n1️⃣3️⃣ TESTING NOTIFICATIONS"

# Notificación de prueba
curl -s -X POST "$BASE_URL/notifications/test" | jq '.' || echo "❌ Test notification failed"

# Notificación a usuario específico
curl -s -X POST "$BASE_URL/notifications/user/testuser123" \
  -H "Content-Type: application/json" \
  -d '{"title": "Test Notification", "message": "This is a test message", "type": 1}' | jq '.' || echo "❌ User notification failed"

# Notificación broadcast
curl -s -X POST "$BASE_URL/notifications/broadcast" \
  -H "Content-Type: application/json" \
  -d '{"title": "Broadcast Test", "message": "This is a broadcast message", "type": 2}' | jq '.' || echo "❌ Broadcast notification failed"

# 14. Test Preferencias de Notificaciones
echo -e "\n1️⃣4️⃣ TESTING NOTIFICATION PREFERENCES"

# Obtener preferencias
curl -s -X GET "$BASE_URL/notificationpreferences/testuser123" | jq '.' || echo "❌ Get preferences failed"

# Actualizar preferencias
curl -s -X PUT "$BASE_URL/notificationpreferences/testuser123" \
  -H "Content-Type: application/json" \
  -d '{"gameEvents": true, "socialEvents": true, "soundEffects": false, "volume": 75}' | jq '.' || echo "❌ Update preferences failed"

echo -e "\n🎉 TESTING COMPLETED!"
echo "==================================================" 