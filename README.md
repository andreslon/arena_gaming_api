# Arena Gaming

A modular monorepo backend application for a Tic Tac Toe game with AI opponent and real-time event system.

## Features

- Tic Tac Toe game against AI opponent using Google's Gemini API
- Real-time event and notification system using Apache Pulsar
- Redis caching for improved performance
- PostgreSQL database for data persistence
- RESTful API endpoints for game and session management
- Social features including friend requests and game invitations

## Technology Stack

- .NET 8
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

## API Endpoints

### Games

- `POST /api/games`: Create a new game
- `GET /api/games/{id}`: Get game by ID
- `POST /api/games/{id}/moves`: Make a move in a game

### Sessions

- `POST /api/games/sessions`: Create a new session
- `POST /api/games/sessions/{id}/games`: Start a new game in a session
- `POST /api/games/sessions/{id}/ai-move`: Make an AI move in the current game

### Notifications

- `POST /api/notifications`: Send a notification
- `POST /api/notifications/social`: Send a social event notification

## Running the Application

1. Start the required services:
   ```bash
   # Start PostgreSQL
   docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres

   # Start Redis
   docker run -d --name redis -p 6379:6379 redis

   # Start Pulsar
   docker run -d --name pulsar -p 6650:6650 -p 8080:8080 apachepulsar/pulsar:latest
   ```

2. Run the application:
   ```bash
   dotnet run --project ArenaGaming.Api
   ```

3. Access the Swagger UI at `https://localhost:5001/swagger`

## Testing

Run the tests using:
```bash
dotnet test
```

## License

MIT 