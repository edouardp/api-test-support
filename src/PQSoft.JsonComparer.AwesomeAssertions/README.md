# PQSoft.JsonComparer.AwesomeAssertions

AwesomeAssertions extensions for JSON comparison that integrate seamlessly with the JsonComparer functionality.

## Why Use This Library?

### The Problem with Traditional JSON Testing

Traditional JSON testing approaches have significant limitations:

```csharp
// ❌ Brittle - breaks when server generates new IDs or timestamps
Assert.Equal("""{"id": "12345", "createdAt": "2024-01-01T10:00:00Z"}""", actualJson);

// ❌ Verbose - requires manual parsing and individual property checks
var parsed = JsonSerializer.Deserialize<ApiResponse>(actualJson);
Assert.Equal("John", parsed.Name);
Assert.NotNull(parsed.Id);
Assert.True(parsed.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
```

### The Solution: Smart JSON Assertions

This library provides **intelligent JSON testing** that handles dynamic values while maintaining strict validation:

```csharp
// ✅ Flexible - extracts dynamic values while validating structure
actualJson.AsJsonString().Should().FullyMatch("""
{
    "id": "[[USER_ID]]",
    "name": "John",
    "createdAt": "{{UTCNOW()}}"
}
""");
```

## Key Testing Scenarios

### 1. API Response Validation
Perfect for testing REST APIs where responses contain generated IDs, timestamps, and tokens:

```csharp
// Test user creation endpoint
string createUserResponse = await httpClient.PostAsync("/users", userData);

createUserResponse.AsJsonString().Should().FullyMatch("""
{
    "user": {
        "id": "[[USER_ID]]",
        "email": "test@example.com",
        "createdAt": "{{UTCNOW()}}",
        "status": "active"
    },
    "authToken": "[[AUTH_TOKEN]]"
}
""");

// Extract values for subsequent tests
var userId = result.ExtractedValues["USER_ID"];
var token = result.ExtractedValues["AUTH_TOKEN"];
```

### 2. Database Integration Testing
Validate database operations while handling auto-generated fields:

```csharp
// After inserting a record
var dbRecord = await repository.GetByIdAsync(newId);
var json = JsonSerializer.Serialize(dbRecord);

json.AsJsonString().Should().FullyMatch("""
{
    "id": "[[RECORD_ID]]",
    "name": "Test Record",
    "createdAt": "{{UTCNOW()}}",
    "updatedAt": "{{UTCNOW()}}",
    "version": 1
}
""");
```

### 3. Event-Driven Architecture Testing
Validate event payloads with dynamic correlation IDs and timestamps:

```csharp
// Validate published event
eventPayload.AsJsonString().Should().FullyMatch("""
{
    "eventId": "{{GUID()}}",
    "correlationId": "[[CORRELATION_ID]]",
    "eventType": "UserCreated",
    "timestamp": "{{UTCNOW()}}",
    "data": {
        "userId": "[[USER_ID]]",
        "email": "user@example.com"
    }
}
""");
```

### 4. Microservices Contract Testing
Ensure service contracts are maintained while allowing for implementation flexibility:

```csharp
// Validate service response contract
serviceResponse.AsJsonString().Should().ContainSubset("""
{
    "status": "success",
    "data": {
        "id": "[[RESOURCE_ID]]",
        "type": "user"
    }
}
""");
// Passes even if response contains additional fields
```

## TimeProvider Integration

### Deterministic Time Testing

The biggest challenge in testing time-sensitive code is dealing with `DateTime.Now` and `DateTime.UtcNow`. This library solves it:

```csharp
// ❌ Flaky test - timing dependent
var response = await api.CreateOrderAsync(order);
var parsed = JsonSerializer.Deserialize<Order>(response);
Assert.True(parsed.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
Assert.True(parsed.CreatedAt < DateTime.UtcNow.AddSeconds(5));
```

```csharp
// ✅ Deterministic test with TimeProvider
var fixedTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));

response.AsJsonString()
    .WithTimeProvider(fixedTime)
    .Should()
    .FullyMatch("""
    {
        "orderId": "[[ORDER_ID]]",
        "createdAt": "{{UTCNOW()}}",
        "expiresAt": "{{UTCNOW()}}"
    }
    """);
```

### Time-Based Function Testing

Test different time formats and time zones consistently:

```csharp
var testTime = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(-5)));

apiResponse.AsJsonString()
    .WithTimeProvider(testTime)
    .Should()
    .FullyMatch("""
    {
        "scheduledAt": "{{NOW()}}",        // Local time with timezone
        "createdUtc": "{{UTCNOW()}}",      // UTC time
        "sessionId": "{{GUID()}}"          // Generated GUID
    }
    """);
```

### Integration with System Clock

For production-like testing, use the system clock:

```csharp
realApiResponse.AsJsonString()
    .WithTimeProvider(TimeProvider.System)
    .Should()
    .FullyMatch("""
    {
        "timestamp": "{{UTCNOW()}}",
        "data": { "status": "processed" }
    }
    """);
```

## Advanced Features

### Token Extraction for Multi-Step Tests

```csharp
// Step 1: Create user and extract ID
var createResult = createUserResponse.AsJsonString()
    .Should()
    .FullyMatch("""{"userId": "[[USER_ID]]", "status": "created"}""");

var userId = createResult.ExtractedValues["USER_ID"];

// Step 2: Use extracted ID in subsequent test
var getUserResponse = await httpClient.GetAsync($"/users/{userId}");
getUserResponse.AsJsonString()
    .Should()
    .ContainSubset("""{"id": "[[SAME_USER_ID]]", "status": "active"}""");
```

### Complex Nested Validation

```csharp
complexApiResponse.AsJsonString().Should().FullyMatch("""
{
    "metadata": {
        "requestId": "{{GUID()}}",
        "timestamp": "{{UTCNOW()}}",
        "version": "1.0"
    },
    "data": {
        "users": [
            {
                "id": "[[USER1_ID]]",
                "profile": {
                    "createdAt": "{{UTCNOW()}}",
                    "settings": {
                        "theme": "dark",
                        "notifications": true
                    }
                }
            }
        ]
    },
    "pagination": {
        "total": "[[TOTAL_COUNT]]",
        "hasMore": false
    }
}
""");
```

## Installation

```bash
dotnet add package PQSoft.JsonComparer.AwesomeAssertions
```

## Dependencies

This package depends on:
- PQSoft.JsonComparer
- AwesomeAssertions
