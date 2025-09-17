# PQSoft.JsonComparer

A powerful library for comparing JSON documents with advanced features.

## Features

- **Exact Match**: Validates that two JSON documents are identical (except for tokens)
- **Subset Match**: Verifies that all elements in the expected JSON exist within the actual JSON
- **Token Support**: Extract values from actual JSON using tokens like `[[TOKEN_NAME]]`
- **Function Execution**: Execute functions like `{{GUID()}}`, `{{NOW()}}`, and `{{UTCNOW()}}` during comparison
- **Variable Substitution**: Substitute variables from provided context during preprocessing
- **Detailed Mismatch Reporting**: Provides structured information on any differences found
- **Custom Function Registration**: Extend functionality with custom functions

## Installation

```bash
dotnet add package PQSoft.JsonComparer
```

## Usage

### Basic Exact Match

```csharp
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
// isMatch = true
// extractedValues["JOBID"] = "12345"
```

### Subset Matching

```csharp
string expectedSubset = """{ "status": "active", "type": "premium" }""";
string actualJson = """{ "id": 123, "name": "John", "status": "active", "type": "premium", "created": "2024-01-01" }""";

bool isSubset = JsonComparer.SubsetMatch(expectedSubset, actualJson, out var tokens, out var errors);
// isSubset = true - all expected fields exist in actual JSON
```

### Token Extraction

```csharp
string template = """{ "userId": "[[USER_ID]]", "orderId": "[[ORDER_ID]]", "total": "[[AMOUNT]]" }""";
string response = """{ "userId": "user_123", "orderId": "order_456", "total": "99.99" }""";

JsonComparer.ExactMatch(template, response, out var tokens, out var mismatches);
// tokens["USER_ID"] = "user_123"
// tokens["ORDER_ID"] = "order_456" 
// tokens["AMOUNT"] = "99.99"
```

### Function Execution

```csharp
string template = """{ 
    "id": "{{GUID()}}", 
    "timestamp": "{{UTCNOW()}}", 
    "date": "{{NOW()}}" 
}""";
string actual = """{ 
    "id": "550e8400-e29b-41d4-a716-446655440000", 
    "timestamp": "2024-01-01T10:00:00.000Z", 
    "date": "2024-01-01T10:00:00.000+00:00" 
}""";

bool matches = JsonComparer.ExactMatch(template, actual, out var values, out var errors);
// Functions are executed and compared against actual values
```

### Variable Substitution

```csharp
var variables = new Dictionary<string, object>
{
    ["BASE_URL"] = "https://api.example.com",
    ["API_VERSION"] = "v1",
    ["USER_ID"] = 12345
};

string expectedJson = """{ 
    "endpoint": "{{BASE_URL}}/{{API_VERSION}}/users/{{USER_ID}}",
    "userId": {{USER_ID}}
}""";
    
string actualJson = """{
    "endpoint": "https://api.example.com/v1/users/12345",
    "userId": 12345
}"""

// Variables are substituted before comparison
bool matches = JsonComparer.ExactMatch(expectedJson, actualJson, variables, out var tokens, out var mismatches);
```

### Custom Functions

```csharp
// Register custom function
JsonComparer.RegisterFunction("CUSTOM_DATE", () => DateTime.Now.ToString("yyyy-MM-dd"));

string template = """{ "processedDate": "{{CUSTOM_DATE()}}" }""";
string actual = """{ "processedDate": "2024-01-01" }""";

bool matches = JsonComparer.ExactMatch(template, actual, out var tokens, out var errors);
```

### Error Handling

```csharp
string expected = """{ "name": "John", "age": 30 }""";
string actual = """{ "name": "Jane", "age": 25 }""";

bool matches = JsonComparer.ExactMatch(expected, actual, out var tokens, out var mismatches);

if (!matches)
{
    foreach (var mismatch in mismatches)
    {
        Console.WriteLine($"Path: {mismatch.Path}");
        Console.WriteLine($"Expected: {mismatch.Expected}");
        Console.WriteLine($"Actual: {mismatch.Actual}");
    }
}
```
