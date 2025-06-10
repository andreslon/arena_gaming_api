# 🎮 Arena Gaming API Endpoints

## 📚 API Endpoints Documentation

### 🎮 **GAMES** - Game Management

#### **POST** `/api/games` - Create new game

**Request:**
```json
{
  "playerId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "board": "         ",
  "currentPlayerSymbol": "X",
  "status": "InProgress",
  "winnerId": null,
  "createdAt": "2025-06-10T03:58:53.644Z",
  "endedAt": null,
  "updatedAt": null
}
```

#### **GET** `/api/games/{id}` - Get game

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "board": "X O      ",
  "currentPlayerSymbol": "X",
  "status": "InProgress",
  "winnerId": null,
  "createdAt": "2025-06-10T03:58:53.644Z",
  "endedAt": null,
  "updatedAt": "2025-06-10T04:01:15.234Z"
}
```

#### **POST** `/api/games/{id}/moves` - Make move

**Request:**
```json
{
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "position": 2
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "board": "X OX     ",
  "currentPlayerSymbol": "O",
  "status": "InProgress",
  "winnerId": null,
  "createdAt": "2025-06-10T03:58:53.644Z",
  "endedAt": null,
  "updatedAt": "2025-06-10T04:02:30.456Z"
}
```

---

### 🎯 **SESSIONS** - Session Management

#### **POST** `/api/sessions` - Create session

**Request:**
```json
{
  "playerId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "status": 0,
  "currentGameId": null,
  "createdAt": "2025-06-10T04:00:00.000Z",
  "endedAt": null,
  "updatedAt": null
}
```

#### **GET** `/api/sessions/{id}` - Get session

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "status": 1,
  "currentGameId": "550e8400-e29b-41d4-a716-446655440001",
  "createdAt": "2025-06-10T04:00:00.000Z",
  "endedAt": null,
  "updatedAt": "2025-06-10T04:01:00.000Z"
}
```

#### **POST** `/api/sessions/{id}/init-game` - Initialize game

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "board": "         ",
  "currentPlayerSymbol": "X",
  "status": "InProgress",
  "winnerId": null,
  "createdAt": "2025-06-10T04:01:00.000Z",
  "endedAt": null,
  "updatedAt": null
}
```

#### **POST** `/api/sessions/{id}/ai-move` - AI move

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "board": "X   O    ",
  "currentPlayerSymbol": "X",
  "status": "InProgress",
  "winnerId": null,
  "createdAt": "2025-06-10T04:01:00.000Z",
  "endedAt": null,
  "updatedAt": "2025-06-10T04:02:00.000Z"
}
```

---

### 🔔 **NOTIFICATIONS** - Notification System

#### **POST** `/api/notifications/user/{userId}` - Send notification to specific user

**Request:**
```json
{
  "title": "New game available",
  "message": "Your opponent has made a move",
  "type": 4,
  "userId": "user123",
  "metadata": {
    "gameId": "550e8400-e29b-41d4-a716-446655440001",
    "priority": "high"
  }
}
```

**Response:**
```json
{
  "success": true,
  "notificationId": "550e8400-e29b-41d4-a716-446655440003"
}
```

#### **POST** `/api/notifications/broadcast` - Broadcast notification

**Request:**
```json
{
  "title": "Scheduled maintenance",
  "message": "System will be under maintenance from 2:00 to 4:00 AM",
  "type": 1,
  "metadata": {
    "maintenanceWindow": "2025-06-11T02:00:00Z"
  }
}
```

**Response:**
```json
{
  "success": true,
  "notificationId": "550e8400-e29b-41d4-a716-446655440004"
}
```

#### **POST** `/api/notifications/queue` - Queue notification

**Request:**
```json
{
  "title": "Achievement unlocked",
  "message": "You've won 10 games in a row!",
  "type": 3,
  "userId": "user123",
  "metadata": {
    "achievement": "winning_streak",
    "level": 10
  }
}
```

**Response:**
```json
{
  "success": true,
  "notificationId": "550e8400-e29b-41d4-a716-446655440005",
  "status": "queued"
}
```

#### **POST** `/api/notifications/test?userId={userId}` - Test notification

**Response:**
```json
{
  "success": true,
  "notificationId": "550e8400-e29b-41d4-a716-446655440006",
  "message": "Test notification sent successfully"
}
```

---

### ⚙️ **NOTIFICATION PREFERENCES** - Notification Preferences

#### **GET** `/api/notificationpreferences/{userId}` - Get preferences

**Response:**
```json
{
  "userId": "user123",
  "gameEvents": true,
  "socialEvents": true,
  "soundEffects": true,
  "volume": 75,
  "emailNotifications": false,
  "pushNotifications": true,
  "tournamentAlerts": true,
  "playerActions": true,
  "systemUpdates": true
}
```

#### **PUT** `/api/notificationpreferences/{userId}` - Update preferences

**Request:**
```json
{
  "gameEvents": true,
  "socialEvents": false,
  "soundEffects": true,
  "volume": 60,
  "emailNotifications": true,
  "pushNotifications": true,
  "tournamentAlerts": false,
  "playerActions": true,
  "systemUpdates": true
}
```

**Response:**
```json
{
  "success": true,
  "message": "Preferences updated successfully"
}
```

#### **POST** `/api/notificationpreferences/{userId}/reset` - Reset to defaults

**Response:**
```json
{
  "success": true,
  "message": "Preferences reset to default values"
}
```

---

## 📊 DTOs and Types

### **CreateGameRequest**
```json
{
  "playerId": "guid"
}
```

### **MakeMoveRequest**
```json
{
  "playerId": "guid",
  "position": "int (0-8)"
}
```

### **GameResponse**
```json
{
  "id": "guid",
  "playerId": "guid",
  "board": "string (9 characters)",
  "currentPlayerSymbol": "string (X or O)",
  "status": "string (InProgress/Ended)",
  "winnerId": "guid or null",
  "createdAt": "datetime",
  "endedAt": "datetime or null",
  "updatedAt": "datetime or null"
}
```

### **CreateSessionRequest**
```json
{
  "playerId": "guid"
}
```

### **SessionResponse**
```json
{
  "id": "guid",
  "playerId": "guid",
  "status": "int (0=Active, 1=Inactive)",
  "currentGameId": "guid or null",
  "createdAt": "datetime",
  "endedAt": "datetime or null",
  "updatedAt": "datetime or null"
}
```

### **CreateNotificationRequest**
```json
{
  "title": "string",
  "message": "string",
  "type": "int (NotificationType)",
  "userId": "string (optional)",
  "metadata": "object (optional)"
}
```

### **NotificationResponse**
```json
{
  "success": "boolean",
  "notificationId": "guid",
  "status": "string (optional)",
  "message": "string (optional)"
}
```

### **NotificationPreferencesResponse**
```json
{
  "userId": "string",
  "gameEvents": "boolean",
  "socialEvents": "boolean",
  "soundEffects": "boolean",
  "volume": "int (0-100)",
  "emailNotifications": "boolean",
  "pushNotifications": "boolean",
  "tournamentAlerts": "boolean",
  "playerActions": "boolean",
  "systemUpdates": "boolean"
}
```

### **UpdateNotificationPreferencesRequest**
```json
{
  "gameEvents": "boolean",
  "socialEvents": "boolean",
  "soundEffects": "boolean",
  "volume": "int (0-100)",
  "emailNotifications": "boolean (optional)",
  "pushNotifications": "boolean (optional)",
  "tournamentAlerts": "boolean (optional)",
  "playerActions": "boolean (optional)",
  "systemUpdates": "boolean (optional)"
}
```

---

## 📋 Enumerations

### **NotificationType**
| Value | Name | Description |
|-------|------|-------------|
| 0 | Info | General information |
| 1 | Warning | Warnings |
| 2 | Error | System errors |
| 3 | Success | Successful operations |
| 4 | GameUpdate | Game updates |
| 5 | TournamentAlert | Tournament alerts |
| 6 | PlayerAction | Player actions |

### **GameStatus**
| Value | Name | Description |
|-------|------|-------------|
| 0 | InProgress | Game in progress |
| 1 | Ended | Game ended |

### **SessionStatus**
| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Active session |
| 1 | Inactive | Inactive session |

---

## 📝 Notes

- **Board positions**: 0-8 representing tic-tac-toe grid
- **Board format**: String of 9 characters ('X', 'O', or ' ')
- **Position mapping**:
  ```
  0 | 1 | 2
  ---------
  3 | 4 | 5
  ---------
  6 | 7 | 8
  ``` 