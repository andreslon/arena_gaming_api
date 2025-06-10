# Arena Gaming API

A modular monorepo backend application for a Tic Tac Toe game with AI opponent and real-time notification system.

## Features

- 🎮 Tic Tac Toe game against AI opponent using Google's Gemini API
- 🔔 Real-time notification system using Apache Pulsar with WebSocket support
- ⚡ Redis caching for improved performance
- 🗄️ PostgreSQL database for data persistence
- 🌐 RESTful API endpoints for game and session management
- 🔧 Auto-testing startup validation for all services
- ⚙️ User notification preferences management
- 📊 Health monitoring endpoints

## Technology Stack

- .NET 9
- Entity Framework Core
- PostgreSQL
- Redis
- Apache Pulsar
- Google Cloud AI Platform (Gemini)
- xUnit for testing

## Project Structure

The solution is organized into three main projects:

1. **ArenaGaming.Api**: Contains the API controllers and configuration
2. **ArenaGaming.Core**: Contains the domain models, interfaces, and application services
3. **ArenaGaming.Infrastructure**: Contains the implementations of interfaces and external service integrations

## Prerequisites

- .NET 8 SDK
- PostgreSQL
- Redis
- Apache Pulsar
- Google Cloud account with Gemini API access

## Configuration

Update the `appsettings.json` file with your configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=arenagaming;Username=postgres;Password=postgres",
    "Redis": "localhost:6379",
    "Pulsar": "pulsar://localhost:6650"
  },
  "Pulsar": {
    "Tenant": "public",
    "Namespace": "default"
  },
  "GoogleCloud": {
    "ProjectId": "your-project-id",
    "Location": "us-central1",
    "ModelId": "gemini-pro"
  }
}
```

## 📋 Frontend JSON Structures

### 🎮 Game Entity

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "board": ["X", " ", "O", " ", "X", " ", " ", " ", "O"],
  "status": 0,
  "currentPlayerSymbol": "X",
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "winnerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "endedAt": "2024-12-19T10:30:00Z",
  "createdAt": "2024-12-19T10:00:00Z",
  "updatedAt": "2024-12-19T10:30:00Z",
  "moves": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "position": 0,
      "symbol": "X",
      "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "timestamp": "2024-12-19T10:15:00Z",
      "createdAt": "2024-12-19T10:15:00Z"
    }
  ]
}
```

**Game Status Enum:**
- `0`: InProgress
- `1`: Ended

### 📱 Session Entity

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "currentGameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "createdAt": "2024-12-19T10:00:00Z",
  "status": 0,
  "endedAt": null,
  "updatedAt": "2024-12-19T10:15:00Z"
}
```

**Session Status Enum:**
- `0`: InProgress
- `1`: Ended

### 🔔 Notification Entity

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Game Update",
  "message": "Your opponent has made a move",
  "type": 4,
  "userId": "user_001",
  "isRead": false,
  "createdAt": "2024-12-19T10:30:00Z",
  "updatedAt": "2024-12-19T10:30:00Z",
  "metadata": {
    "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "round": 3
  }
}
```

**Notification Type Enum:**
- `0`: Info
- `1`: Warning
- `2`: Error
- `3`: Success
- `4`: GameUpdate
- `5`: TournamentAlert
- `6`: PlayerAction

### ⚙️ Notification Preferences Entity

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "user_001",
  "gameEvents": true,
  "socialEvents": true,
  "soundEffects": true,
  "volume": 75,
  "emailNotifications": false,
  "pushNotifications": true,
  "tournamentAlerts": true,
  "playerActions": true,
  "systemUpdates": true,
  "createdAt": "2024-12-19T10:00:00Z",
  "updatedAt": "2024-12-19T10:30:00Z"
}
```

## 🌐 API Endpoints

### 🎮 Games

- `POST /api/games`: Create a new game
- `GET /api/games/{id}`: Get game by ID  
- `POST /api/games/{id}/moves`: Make a move in a game

**Create Game Request:**
```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Make Move Request:**
```json
{
  "position": 4,
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 📱 Sessions

- `POST /api/sessions`: Create a new session
- `GET /api/sessions/{id}`: Get session by ID
- `POST /api/sessions/{id}/init-game`: Initialize a new game in session
- `POST /api/sessions/{id}/ai-move`: Make an AI move

**Create Session Request:**
```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 🔔 Notifications

- `POST /api/notifications/user/{userId}`: Send notification to specific user
- `POST /api/notifications/broadcast`: Send broadcast notification
- `POST /api/notifications/queue`: Queue notification for processing
- `POST /api/notifications/test`: Send test notification

**Send Notification Request:**
```json
{
  "title": "Game Update",
  "message": "Your turn to play!",
  "type": 4,
  "userId": "user_001",
  "metadata": {
    "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
}
```

### ⚙️ Notification Preferences

- `GET /api/notificationpreferences/{userId}`: Get user preferences
- `PUT /api/notificationpreferences/{userId}`: Update user preferences  
- `POST /api/notificationpreferences/{userId}/reset`: Reset to default preferences

**Update Preferences Request:**
```json
{
  "gameEvents": true,
  "socialEvents": false,
  "soundEffects": true,
  "volume": 80,
  "emailNotifications": false,
  "pushNotifications": true,
  "tournamentAlerts": true,
  "playerActions": true,
  "systemUpdates": false
}
```

### 🏥 Health Monitoring

- `GET /api/health`: Overall system health
- `GET /api/health/postgresql`: PostgreSQL health
- `GET /api/health/redis`: Redis health
- `GET /api/health/pulsar`: Pulsar health
- `GET /api/health/startup`: Startup validation results
- `POST /api/health/postgresql/test`: Full PostgreSQL test
- `POST /api/health/redis/test`: Full Redis test  
- `POST /api/health/pulsar/test`: Full Pulsar test

## 🚀 Running the Application

### Prerequisites

- .NET 9 SDK
- Docker (for services)

### 1. Start Required Services

```bash
# Start PostgreSQL
docker run -d --name postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=arenagaming \
  -p 5432:5432 postgres:15

# Start Redis
docker run -d --name redis -p 6379:6379 redis:7-alpine

# Start Pulsar
docker run -d --name pulsar \
  -p 6650:6650 -p 8080:8080 \
  apachepulsar/pulsar:latest \
  bin/pulsar standalone
```

### 2. Run the Application

```bash
# Clone and navigate to project
git clone <repository-url>
cd arena_gaming_api

# Run the application
dotnet run --project ArenaGaming.Api
```

### 3. Verify Auto-Testing

The application automatically validates all services on startup. Look for logs like:

```
🚀 Starting automatic health checks for all services...
✅ PostgreSQL  | PostgreSQL is healthy and ready (156ms)
✅ Redis       | Redis is healthy and ready (32ms)  
✅ Pulsar      | Pulsar is healthy and ready (1250ms)
🎉 All systems operational - Application ready for production use!
```

### 4. Access the API

- **Swagger UI**: `https://localhost:5001/swagger`
- **Health Check**: `GET https://localhost:5001/api/health`
- **Startup Status**: `GET https://localhost:5001/api/health/startup`

## 🔔 Real-time Notifications

The application supports real-time notifications via Pulsar WebSockets:

### Pulsar Topics
- `notifications`: General notifications
- `user-notifications-{userId}`: User-specific notifications  
- `broadcast-notifications`: System-wide announcements
- `game-updates-{userId}`: Game state changes
- `tournament-alerts`: Tournament notifications
- `player-actions-{userId}`: Player action notifications

### WebSocket Connection Example

```javascript
// Frontend WebSocket connection to Pulsar
const pulsarWs = new WebSocket('ws://localhost:8080/ws/v2/consumer/persistent/public/default/user-notifications-user_001/subscription-name');

pulsarWs.onmessage = function(event) {
  const notification = JSON.parse(event.data);
  console.log('Received notification:', notification);
};
```

## 🧪 Testing

### Unit Tests
```bash
dotnet test
```

### API Testing with cURL

**Create a new game:**
```bash
curl -X POST "https://localhost:5001/api/games" \
  -H "Content-Type: application/json" \
  -d '{"playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
```

**Make a move:**
```bash
curl -X POST "https://localhost:5001/api/games/{gameId}/moves" \
  -H "Content-Type: application/json" \
  -d '{"position": 4, "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
```

**Get user notification preferences:**
```bash
curl -X GET "https://localhost:5001/api/notificationpreferences/user_001"
```

**Send a test notification:**
```bash
curl -X POST "https://localhost:5001/api/notifications/test?userId=user_001"
```

### Health Check Testing
```bash
# Check overall health
curl -X GET "https://localhost:5001/api/health"

# Check startup validation results
curl -X GET "https://localhost:5001/api/health/startup"

# Run full service tests
curl -X POST "https://localhost:5001/api/health/postgresql/test"
curl -X POST "https://localhost:5001/api/health/redis/test"  
curl -X POST "https://localhost:5001/api/health/pulsar/test"
```

## 🐳 Docker Support

### Build and Run with Docker

```bash
# Build the image
docker build -t arena-gaming-api .

# Run the container
docker run -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=arenagaming;Username=postgres;Password=postgres" \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  -e ConnectionStrings__Pulsar="pulsar://host.docker.internal:6650" \
  arena-gaming-api
```

### Docker Compose (Full Stack)

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: arenagaming
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
      
  pulsar:
    image: apachepulsar/pulsar:latest
    command: bin/pulsar standalone
    ports:
      - "6650:6650"
      - "8080:8080"
      
  arena-gaming-api:
    build: .
    ports:
      - "5000:8080"
    depends_on:
      - postgres
      - redis
      - pulsar
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=arenagaming;Username=postgres;Password=postgres
      - ConnectionStrings__Redis=redis:6379
      - ConnectionStrings__Pulsar=pulsar://pulsar:6650
```

## 📱 Frontend Integration Guide

### Key Points for Frontend Development

1. **Game Board**: Array of 9 positions (0-8), values are 'X', 'O', or ' '
2. **Position Mapping**: 
   ```
   0 | 1 | 2
   ---------
   3 | 4 | 5
   ---------
   6 | 7 | 8
   ```
3. **Real-time Updates**: Subscribe to WebSocket topics for live notifications
4. **User Preferences**: Volume control (0-100), boolean toggles for notification types
5. **Auto Health**: Application validates all services on startup automatically

### Recommended Flow
1. Create session → Get session ID
2. Initialize game in session → Get game ID  
3. Subscribe to user-specific notification topics
4. Make moves and listen for AI responses
5. Handle game end states and start new games

## 📄 License

MIT 