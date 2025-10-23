# PQSoft.JsonComparer

A powerful library for comparing JSON documents with advanced features.

## Features

- **Exact Match**: Validates that two JSON documents are identical (except for
  tokens)
- **Subset Match**: Verifies that all elements in the expected JSON exist within
  the actual JSON
- **Token Support**: Extract values from actual JSON using tokens like
  `[[TOKEN_NAME]]`
- **Function Execution**: Execute functions like `{{GUID()}}`, `{{NOW()}}`, and
  `{{UTCNOW()}}` during comparison
- **Variable Substitution**: Substitute variables from provided context during
  preprocessing
- **Detailed Mismatch Reporting**: Provides structured information on any
  differences found
- **Custom Function Registration**: Extend functionality with custom functions

## Usage

### Basic Comparison

```csharp
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
// isMatch = true
// extractedValues["JOBID"] = "12345"
```

### Subset Matching

```csharp
string expectedJson = """{ "status": "complete" }""";
string actualJson = """{ "id": "12345", "status": "complete", "timestamp": "2024-01-01" }""";

bool isMatch = JsonComparer.SubsetMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
// isMatch = true - expected properties exist in actual
```

### Built-in Functions

- `{{GUID()}}` - Matches any GUID format
- `{{NOW()}}` - Matches current date/time with tolerance
- `{{UTCNOW()}}` - Matches current UTC date/time with tolerance

### Custom Functions

```csharp
JsonComparer.RegisterFunction("CUSTOM", (value) => value.StartsWith("prefix"));
```
