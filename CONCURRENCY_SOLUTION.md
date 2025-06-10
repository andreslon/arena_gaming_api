# Concurrency Conflict Solution

## Problem
The API was experiencing 409 Conflict errors when multiple users tried to make moves simultaneously in the game. The error message was:

```json
{
    "error": "Concurrency conflict",
    "message": "The resource was modified by another operation. Please retry your request.",
    "details": "The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded.",
    "statusCode": 409
}
```

## Root Cause
The issue was caused by Entity Framework's optimistic concurrency control without proper configuration. When multiple requests tried to update the same game entity simultaneously:

1. Request A loads game state
2. Request B loads the same game state  
3. Request A modifies and saves successfully
4. Request B tries to save but fails because the entity was changed since it was loaded
5. Entity Framework throws `DbUpdateConcurrencyException`
6. The exception middleware converts this to a 409 Conflict response

## Solution Implemented

### 1. Added Optimistic Concurrency Control
- Added `RowVersion` property to the base `Entity` class using the `[Timestamp]` attribute
- Configured Entity Framework to use `RowVersion` as a concurrency token for all entities
- Created a database migration to add `RowVersion` columns to all tables

### 2. Implemented Retry Logic
- Added retry mechanism in `GameService.MakeMoveAsync()` with exponential backoff
- Maximum of 3 retries with delays of 100ms, 200ms, and 300ms
- Graceful handling of concurrency exceptions

### 3. Enhanced Error Handling
- Improved error messages in the API controller
- Specific handling for concurrency conflicts vs other validation errors
- Better user-facing error messages

### 4. Pre-validation
- Added move validation before attempting database updates
- Reduces unnecessary database operations for invalid moves

## Code Changes

### Entity Base Class
```csharp
[Timestamp]
public byte[]? RowVersion { get; set; }
```

### Entity Framework Configuration
```csharp
entity.Property(e => e.RowVersion).IsRowVersion();
```

### Service Layer Retry Logic
```csharp
const int maxRetries = 3;
var retryCount = 0;

while (retryCount < maxRetries)
{
    try
    {
        // Load fresh data, validate, update
        return game;
    }
    catch (Exception ex) when (IsConcurrencyException(ex))
    {
        retryCount++;
        if (retryCount >= maxRetries)
        {
            throw new InvalidOperationException(
                "Unable to complete the move due to concurrent modifications. Please try again.");
        }
        await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount), cancellationToken);
    }
}
```

### Controller Error Handling
```csharp
catch (InvalidOperationException ex) when (ex.Message.Contains("concurrent modifications"))
{
    return Conflict(new 
    { 
        error = "Concurrency conflict", 
        message = "The game was modified by another player. Please refresh and try again.",
        details = ex.Message 
    });
}
```

## Database Migration
A migration `AddConcurrencyControl` was created to add `RowVersion` columns to all entity tables:

```sql
ALTER TABLE "Games" ADD "RowVersion" bytea;
ALTER TABLE "Moves" ADD "RowVersion" bytea;
ALTER TABLE "Sessions" ADD "RowVersion" bytea;
-- etc.
```

## Benefits

1. **Automatic Conflict Detection**: Entity Framework now properly detects when entities have been modified
2. **Graceful Recovery**: Retry logic allows most conflicts to be resolved automatically
3. **Better User Experience**: Clear error messages when conflicts can't be resolved
4. **Data Integrity**: Prevents lost updates and ensures game state consistency
5. **Performance**: Pre-validation reduces unnecessary database operations

## Usage
The solution is transparent to API consumers. In case of concurrent modifications:

1. **Best Case**: Conflict is resolved automatically through retry logic
2. **Worst Case**: User gets a clear 409 error message asking them to refresh and try again

## Testing
To test the solution:

1. Make simultaneous move requests to the same game
2. Observe that most conflicts are resolved automatically
3. In high-contention scenarios, users receive clear error messages instead of cryptic database errors

## Future Enhancements

1. **Pessimistic Locking**: For high-contention scenarios, consider database-level locking
2. **Queue-based Processing**: For turn-based games, consider message queues for move processing
3. **Real-time Updates**: WebSocket notifications when game state changes
4. **Telemetry**: Add logging and metrics for concurrency conflict frequency 