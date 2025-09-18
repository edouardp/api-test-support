# PQSoft Open Source

A repository for Open Source packages for API Test Engineering

## Packages

### PQSoft.HttpFile

A library for parsing HTTP request and response files, similar to Microsoft's
VS Code .http files. It provides functionality to:

- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Semantically compare parsed headers for testing purposes
- Convert parsed requests to `HttpRequestMessage` objects for integration
  with HttpClient

#### Converting to HttpRequestMessage

The `ParsedHttpRequest` record includes a `ToHttpRequestMessage()` method that
converts the parsed request into a standard .NET `HttpRequestMessage`:

```csharp
// Parse an HTTP request from a stream
var rawHttp = """
    POST https://api.example.com/users HTTP/1.1
    Content-Type: application/json
    Authorization: Bearer token123

    {"name": "John", "age": 30}
    """;

using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
var parsedRequest = await HttpStreamParser.ParseAsync(stream);

// Convert to HttpRequestMessage for use with HttpClient
var httpRequest = parsedRequest.ToHttpRequestMessage();

// Use with HttpClient
using var client = new HttpClient();
var response = await client.SendAsync(httpRequest);
```

The method handles:
- Setting HTTP method and URL
- Adding all headers to the request
- Setting request content when a body is present
- Proper header placement (request headers vs content headers)

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

## Installation

Install packages from NuGet:

```bash
# HTTP file parsing
dotnet add package PQSoft.HttpFile

# JSON comparison
dotnet add package PQSoft.JsonComparer

# JSON comparison with FluentAssertions
dotnet add package PQSoft.JsonComparer.AwesomeAssertions
```

## Building and Publishing

### Prerequisites

1. **NuGet API Key**: Create an API key at [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
2. **Environment Variable**: Set your API key as an environment variable:
   ```bash
   export NUGET_API_KEY="your-api-key-here"
   ```

### Version Management

All packages use centralized versioning in `Directory.Build.props`. To update the version:

1. **Edit Directory.Build.props**:
   ```xml
   <Version>1.0.1</Version>
   ```

2. **Or override during build**:
   ```bash
   ./build-nuget.sh 1.0.1
   ```

### Building and Publishing

Use the automated build script:

```bash
# Build and publish all packages with version 1.0.1
./build-nuget.sh 1.0.1
```

The script will:
- Validate version parameter and API key
- Clean and build the solution
- Pack all packages with the specified version
- Upload packages to NuGet.org in dependency order

### Manual Building

For development or testing:

```bash
# Build solution
dotnet build --configuration Release

# Pack individual packages
dotnet pack src/PQSoft.HttpFile/PQSoft.HttpFile.csproj --configuration Release -p:Version=1.0.1
dotnet pack src/PQSoft.JsonComparer/PQSoft.JsonComparer.csproj --configuration Release -p:Version=1.0.1
dotnet pack src/PQSoft.JsonComparer.AwesomeAssertions/PQSoft.JsonComparer.AwesomeAssertions.csproj --configuration Release -p:Version=1.0.1

# Push to NuGet (in dependency order)
dotnet nuget push "src/PQSoft.HttpFile/bin/Release/PQSoft.HttpFile.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push "src/PQSoft.JsonComparer/bin/Release/PQSoft.JsonComparer.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push "src/PQSoft.JsonComparer.AwesomeAssertions/bin/Release/PQSoft.JsonComparer.AwesomeAssertions.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

### Package Dependencies

- **PQSoft.HttpFile**: No external dependencies
- **PQSoft.JsonComparer**: Depends on System.Text.Json
- **PQSoft.JsonComparer.AwesomeAssertions**: Depends on PQSoft.JsonComparer and AwesomeAssertions

Packages must be uploaded in dependency order to ensure successful publication.

## Support Projects

PQSoft.HttpFile.UnitTests
PQSoft.JsonComparer.UnitTests
PQSoft.JsonComparer.AwesomeAssertions.UnitTests

