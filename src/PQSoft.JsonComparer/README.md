# PQSoft.JsonComparer

A powerful .NET library for comparing JSON documents with advanced features including token extraction, function execution, and variable substitution. Perfect for API testing scenarios where you need flexible JSON validation.

## Features

- **Exact Match**: Validates that two JSON documents are identical (except for tokens)
- **Subset Match**: Verifies that all elements in the expected JSON exist within the actual JSON
- **Token Support**: Extract values from actual JSON using tokens like `[[TOKEN_NAME]]`
- **Function Execution**: Execute functions like `{{GUID()}}`, `{{NOW()}}`, and `{{UTCNOW()}}` during comparison

- **Detailed Mismatch Reporting**: Provides structured information on any differences found
- **Custom Function Registration**: Extend functionality with custom functions

## Installation

```bash
dotnet add package PQSoft.JsonComparer
```

## Quick Start

### Basic Exact Match

```csharp
using PQSoft.JsonComparer;

string expectedJson = """{"name": "John", "age": 30}""";
string actualJson = """{"name": "John", "age": 30}""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson);
// isMatch = true
```

### Token Extraction

```csharp
using PQSoft.JsonComparer;

string expectedJson = """{ "id": "[[JOBID]]", "status": "complete" }""";
string actualJson = """{ "id": "12345", "status": "complete" }""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
// isMatch = true
// extractedValues["JOBID"] = "12345"
```

### Function Execution

```csharp
using PQSoft.JsonComparer;

string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
// isMatch = true (assuming NOW() generates a matching timestamp)
// extractedValues["JOBID"] = "12345"
```

## Advanced Usage

### Subset Matching

Use subset matching when you only care about certain fields being present:

```csharp
string expectedJson = """{ "name": "John", "status": "active" }""";
string actualJson = """{ "id": 123, "name": "John", "age": 30, "status": "active", "email": "john@example.com" }""";

bool isMatch = JsonComparer.SubsetMatch(expectedJson, actualJson);
// isMatch = true (all expected fields are present in actual)
```



### Custom Functions

Register your own functions for dynamic value generation:

```csharp
JsonComparer.RegisterFunction("RANDOM_EMAIL", () => $"user{Random.Next(1000)}@example.com");

string expectedJson = """{ "email": "{{RANDOM_EMAIL()}}" }""";
string actualJson = """{ "email": "user123@example.com" }""";

// The function will generate a random email for comparison
```

### Detailed Mismatch Information

```csharp
string expectedJson = """{ "name": "John", "age": 30, "status": "active" }""";
string actualJson = """{ "name": "Jane", "age": 25, "status": "active" }""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);

if (!isMatch)
{
    foreach (var mismatch in mismatches)
    {
        Console.WriteLine($"Path: {mismatch.Path}");
        Console.WriteLine($"Expected: {mismatch.Expected}");
        Console.WriteLine($"Actual: {mismatch.Actual}");
        Console.WriteLine($"Type: {mismatch.MismatchType}");
    }
}
```

## Built-in Functions

The library comes with several built-in functions:

- `{{GUID()}}`: Generates a new GUID
- `{{NOW()}}`: Current date/time in ISO 8601 format
- `{{UTCNOW()}}`: Current UTC date/time in ISO 8601 format
- `{{TODAY()}}`: Current date in YYYY-MM-DD format
- `{{TIMESTAMP()}}`: Current Unix timestamp

## API Reference

### JsonComparer

Static methods:
- `ExactMatch(string expected, string actual)`: Basic exact match comparison
- `ExactMatch(string expected, string actual, out Dictionary<string, object> extractedValues, out List<JsonMismatch> mismatches)`: Exact match with token extraction and mismatch details

- `SubsetMatch(string expected, string actual)`: Basic subset match comparison
- `SubsetMatch(string expected, string actual, out Dictionary<string, object> extractedValues, out List<JsonMismatch> mismatches)`: Subset match with token extraction and mismatch details
- `RegisterFunction(string name, Func<object> function)`: Register custom functions

### JsonMismatch

Properties:
- `Path`: JSON path where the mismatch occurred
- `Expected`: Expected value
- `Actual`: Actual value  
- `MismatchType`: Type of mismatch (ValueMismatch, MissingProperty, ExtraProperty, etc.)

## Token Syntax

- `[[TOKEN_NAME]]`: Extracts the value at this location and stores it with the given name
- `{{FUNCTION()}}`: Executes the named function and uses its return value for comparison


## Use Cases

This library is perfect for:

- **API Testing**: Validate API responses with dynamic values
- **Integration Testing**: Compare expected vs actual JSON with flexible matching
- **Data Validation**: Ensure JSON data meets expected structure and content requirements
- **Test Automation**: Extract values from responses for use in subsequent tests

## Contributing

Contributions are welcome! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
