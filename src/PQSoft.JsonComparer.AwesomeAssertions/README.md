# PQSoft.JsonComparer.AwesomeAssertions

AwesomeAssertions extensions for JSON comparison that integrate seamlessly with the JsonComparer functionality.

## Features

- Provides a fluent API for JSON assertions
- Works with the token extraction features of JsonComparer
- Supports both exact match and subset match assertions
- Integrates with AwesomeAssertions failure reporting

## Installation

```bash
dotnet add package PQSoft.JsonComparer.AwesomeAssertions
```

## Usage

### Basic Fluent Assertions

```csharp
string actualJson = """{ "id": "12345", "name": "John", "status": "active" }""";
string expectedJson = """{ "id": "[[USER_ID]]", "name": "John", "status": "active" }""";

actualJson.AsJsonString().Should().FullyMatch(expectedJson);
// Assertion passes and USER_ID token is extracted
```

### Subset Matching

```csharp
string actualJson = """{
    "id": 123,
    "name": "John",
    "email": "john@example.com",
    "status": "active",
    "created": "2024-01-01T10:00:00Z"
}""";

string expectedSubset = """{ "name": "John", "status": "active" }""";

actualJson.AsJsonString().Should().ContainSubset(expectedSubset);
// Passes - actual JSON contains all expected fields
```

### Token Extraction with Assertions

```csharp
string apiResponse = """{
    "orderId": "order_12345",
    "userId": "user_67890",
    "total": 99.99,
    "status": "completed"
}""";

string template = """{
    "orderId": "[[ORDER_ID]]",
    "userId": "[[USER_ID]]",
    "total": "[[TOTAL]]",
    "status": "completed"
}""";

var result = apiResponse.AsJsonString().Should().FullyMatch(template);

// Access extracted tokens
var orderId = result.ExtractedTokens["ORDER_ID"];  // "order_12345"
var userId = result.ExtractedTokens["USER_ID"];    // "user_67890"
var total = result.ExtractedTokens["TOTAL"];       // "99.99"
```

### Function-Based Matching

```csharp
string actualResponse = """{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "createdAt": "2024-01-01T10:00:00.000Z",
    "processedAt": "2024-01-01T10:05:30.123+00:00"
}""";

string expectedTemplate = """{
    "id": "{{GUID()}}",
    "createdAt": "{{UTCNOW()}}",
    "processedAt": "{{NOW()}}"
}""";

actualResponse.AsJsonString().Should().FullyMatch(expectedTemplate);
// Functions are executed and validated against actual values
```

### Complex API Testing

```csharp
// Test API response structure and extract values for further testing
string createUserResponse = """{
    "user": {
        "id": "usr_abc123",
        "email": "test@example.com",
        "profile": {
            "firstName": "John",
            "lastName": "Doe"
        }
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-12-31T23:59:59Z"
}""";

string expectedStructure = """{
    "user": {
        "id": "[[USER_ID]]",
        "email": "test@example.com",
        "profile": {
            "firstName": "John",
            "lastName": "Doe"
        }
    },
    "token": "[[AUTH_TOKEN]]",
    "expiresAt": "{{UTCNOW()}}"
}""";

var result = createUserResponse.AsJsonString().Should().FullyMatch(expectedStructure);

// Use extracted tokens in subsequent tests
var userId = result.ExtractedTokens["USER_ID"];
var authToken = result.ExtractedTokens["AUTH_TOKEN"];
```

### Error Assertions

```csharp
string errorResponse = """{ "error": "Invalid request", "code": 400 }""";
string expectedError = """{ "error": "Invalid request", "code": 400 }""";

errorResponse.AsJsonString().Should().FullyMatch(expectedError);
```

### Array and Complex Object Matching

```csharp
string actualJson = """{
    "users": [
        { "id": "1", "name": "John" },
        { "id": "2", "name": "Jane" }
    ],
    "total": 2
}""";

string expectedPattern = """{
    "users": [
        { "id": "[[USER1_ID]]", "name": "John" },
        { "id": "[[USER2_ID]]", "name": "Jane" }
    ],
    "total": 2
}""";

var result = actualJson.AsJsonString().Should().FullyMatch(expectedPattern);
// Extracts USER1_ID = "1" and USER2_ID = "2"
```

## Dependencies

This package depends on:
- PQSoft.JsonComparer
- AwesomeAssertions
