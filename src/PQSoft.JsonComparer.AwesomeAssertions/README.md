# PQSoft.JsonComparer.AwesomeAssertions

FluentAssertions extensions for JSON comparison that integrate seamlessly with PQSoft.JsonComparer functionality. This package provides a fluent API for JSON assertions with token extraction, function execution, and detailed failure reporting.

## Features

- **Fluent API**: Natural, readable assertions using FluentAssertions syntax
- **Token Extraction**: Access extracted values directly from assertion results
- **Function Support**: Full support for PQSoft.JsonComparer functions like `{{NOW()}}`, `{{GUID()}}`
- **Exact and Subset Matching**: Both comparison modes available through fluent interface
- **Rich Failure Messages**: Detailed error messages showing exactly what didn't match

- **Seamless Integration**: Works perfectly with existing FluentAssertions test suites

## Installation

```bash
dotnet add package PQSoft.JsonComparer.AwesomeAssertions
```

Note: This package depends on both `PQSoft.JsonComparer` and `FluentAssertions`, which will be installed automatically.

## Quick Start

### Basic Usage

```csharp
using FluentAssertions;
using PQSoft.JsonComparer.AwesomeAssertions;

string actualJson = """{"name": "John", "age": 30}""";
string expectedJson = """{"name": "John", "age": 30}""";

actualJson.AsJsonString().Should().FullyMatch(expectedJson);
```

### Token Extraction

```csharp
using FluentAssertions;
using PQSoft.JsonComparer.AwesomeAssertions;

string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";

var result = actualJson.AsJsonString().Should().FullyMatch(expectedJson);

// Access extracted values
string jobId = result.ExtractedValues["JOBID"].ToString();
Assert.Equal("12345", jobId);
```

### Subset Matching

```csharp
string actualJson = """{ "id": 123, "name": "John", "age": 30, "status": "active", "email": "john@example.com" }""";
string expectedJson = """{ "name": "John", "status": "active" }""";

actualJson.AsJsonString().Should().ContainSubset(expectedJson);
```

## Advanced Usage

### Custom Assertion Messages

```csharp
string actualJson = """{"name": "Jane", "age": 25}""";
string expectedJson = """{"name": "John", "age": 30}""";

actualJson.AsJsonString().Should().FullyMatch(expectedJson, "because the API should return the correct user data");
```

### Working with HTTP Responses

Perfect for API testing scenarios:

```csharp
// In your test method
var response = await httpClient.GetAsync("/api/users/123");
var responseBody = await response.Content.ReadAsStringAsync();

string expectedJson = """
{
    "id": "[[USER_ID]]",
    "name": "John Doe",
    "email": "john@example.com",
    "createdAt": "{{NOW()}}",
    "isActive": true
}""";

var result = responseBody.AsJsonString().Should().FullyMatch(expectedJson);

// Use extracted values in subsequent tests
string userId = result.ExtractedValues["USER_ID"].ToString();
// ... use userId in other API calls
```

### Chaining Assertions

```csharp
string actualJson = """
{
    "users": [
        {"id": "123", "name": "John", "role": "admin"},
        {"id": "456", "name": "Jane", "role": "user"}
    ],
    "total": 2,
    "page": 1
}""";

string expectedJson = """
{
    "users": [
        {"id": "[[ADMIN_ID]]", "name": "John", "role": "admin"},
        {"id": "[[USER_ID]]", "name": "Jane", "role": "user"}
    ],
    "total": 2
}""";

var result = actualJson.AsJsonString().Should().ContainSubset(expectedJson);

// Chain additional assertions
result.ExtractedValues["ADMIN_ID"].ToString().Should().NotBeNullOrEmpty();
result.ExtractedValues["USER_ID"].ToString().Should().NotBeNullOrEmpty();
```

## API Reference

### Extension Methods

#### AsJsonString()
Converts a string to a `JsonStringAssertions` object for fluent assertions.

```csharp
string json = """{"key": "value"}""";
var assertions = json.AsJsonString();
```

#### FullyMatch()
Performs exact match comparison between JSON documents.

```csharp
// Basic usage
actualJson.AsJsonString().Should().FullyMatch(expectedJson);

// With custom message
actualJson.AsJsonString().Should().FullyMatch(expectedJson, "because of business logic");
```

#### ContainSubset()
Performs subset match comparison, ensuring expected elements exist in actual JSON.

```csharp
// Basic usage
actualJson.AsJsonString().Should().ContainSubset(expectedJson);

// With custom message
actualJson.AsJsonString().Should().ContainSubset(expectedJson, "because the response should include required fields");
```

### JsonMatchResult

Both `FullyMatch()` and `ContainSubset()` return a `JsonMatchResult` object with:

- `ExtractedValues`: Dictionary containing all extracted token values
- `Mismatches`: List of any mismatches found (empty for successful matches)

## Integration with Test Frameworks

### xUnit Example

```csharp
[Fact]
public async Task GetUser_ShouldReturnExpectedFormat()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/users/123");
    var content = await response.Content.ReadAsStringAsync();
    
    // Assert
    string expectedJson = """
    {
        "id": "[[USER_ID]]",
        "name": "John Doe",
        "email": "{{EMAIL()}}",
        "createdAt": "{{NOW()}}",
        "isActive": true
    }""";
    
    var result = content.AsJsonString().Should().FullyMatch(expectedJson);
    
    // Additional assertions using extracted values
    result.ExtractedValues["USER_ID"].ToString().Should().Be("123");
}
```

### NUnit Example

```csharp
[Test]
public async Task GetUser_ShouldReturnExpectedFormat()
{
    // Arrange & Act
    var responseJson = await GetUserFromApi("123");
    
    // Assert
    string expectedJson = """{"id": "[[ID]]", "status": "active"}""";
    
    var result = responseJson.AsJsonString().Should().FullyMatch(expectedJson);
    
    Assert.That(result.ExtractedValues["ID"].ToString(), Is.EqualTo("123"));
}
```

## Error Messages

The library provides detailed error messages when assertions fail:

```
Expected JSON to fully match:
{
  "name": "John",
  "age": 30
}

But found differences:
- At path '$.name': Expected 'John' but was 'Jane'
- At path '$.age': Expected 30 but was 25

because the API should return the correct user data
```

## Contributing

Contributions are welcome! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
