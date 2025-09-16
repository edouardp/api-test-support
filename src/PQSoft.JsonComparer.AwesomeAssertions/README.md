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

```csharp
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";

actualJson.AsJsonString().Should().FullyMatch(expectedJson);
// Extracted values are available in the assertion result
```

## Dependencies

This package depends on:
- PQSoft.JsonComparer
- AwesomeAssertions
