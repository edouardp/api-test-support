# PQSoft Open Source

A repository for Open Source packages for API Test Engineering

## Packages

### PQSoft.HttpFile

A library for parsing HTTP request and response files, similar to Microsoft's VS Code .http files. It provides functionality to:
- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Semantically compare parsed headers for testing purposes

This package is particularly useful for API testing scenarios where you need to parse and validate HTTP traffic.

### PQSoft.JsonComparer

A powerful library for comparing JSON documents with advanced features:
- **Exact Match**: Validates that two JSON documents are identical (except for tokens)
- **Subset Match**: Verifies that all elements in the expected JSON exist within the actual JSON
- **Token Support**: Extract values from actual JSON using tokens like `[[TOKEN_NAME]]`
- **Function Execution**: Execute functions like `{{GUID()}}`, `{{NOW()}}`, and `{{UTCNOW()}}` during comparison
- **Variable Substitution**: Substitute variables from provided context during preprocessing
- **Detailed Mismatch Reporting**: Provides structured information on any differences found
- **Custom Function Registration**: Extend functionality with custom functions

Example:
```csharp
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";

bool isMatch = JsonComparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
// isMatch = true
// extractedValues["JOBID"] = "12345"
```

### PQSoft.JsonComparer.AwesomeAssertions

FluentAssertions extensions for JSON comparison that integrate seamlessly with the JsonComparer functionality:
- Provides a fluent API for JSON assertions
- Works with the token extraction features of JsonComparer
- Supports both exact match and subset match assertions
- Integrates with FluentAssertions failure reporting

Example:
```csharp
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";

actualJson.AsJsonString().Should().FullyMatch(expectedJson);
// Extracted values are available in the assertion result
```

## Support Projects

PQSoft.HttpFile.UnitTests
PQSoft.JsonComparer.UnitTests
PQSoft.JsonComparer.AwesomeAssertions.UnitTests



