# PQSoft.JsonComparer.AwesomeAssertions

FluentAssertions extensions for JSON comparison that integrate seamlessly with the JsonComparer functionality.

## Features

- Provides a fluent API for JSON assertions
- Works with the token extraction features of JsonComparer
- Supports both exact match and subset match assertions
- Integrates with FluentAssertions failure reporting

## Usage

### Exact Match Assertions

```csharp
string actualJson = """{ "id": "12345", "createdAt": "2024-01-01T10:00:00.000+00:00", "status": "complete" }""";
string expectedJson = """{ "id": "[[JOBID]]", "createdAt": "{{NOW()}}", "status": "complete" }""";

actualJson.AsJsonString().Should().FullyMatch(expectedJson);
// Extracted values are available in the assertion result
```

### Subset Match Assertions

```csharp
string actualJson = """{ "id": "12345", "status": "complete", "timestamp": "2024-01-01" }""";
string expectedJson = """{ "status": "complete" }""";

actualJson.AsJsonString().Should().ContainSubset(expectedJson);
```

### Working with Extracted Values

```csharp
var result = actualJson.AsJsonString().Should().FullyMatch(expectedJson);
var jobId = result.ExtractedValues["JOBID"]; // "12345"
```

### Integration with Test Frameworks

Works seamlessly with popular test frameworks:

```csharp
[Test]
public void Should_Match_Expected_Response()
{
    var response = await httpClient.GetStringAsync("/api/jobs/123");
    var expected = """{ "id": "[[JOB_ID]]", "status": "completed", "completedAt": "{{NOW()}}" }""";

    response.AsJsonString().Should().FullyMatch(expected);
}
```
