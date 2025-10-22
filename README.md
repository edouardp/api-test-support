# PQSoft Open Source

A repository for Open Source packages for API Test Engineering

## Packages

### PQSoft.HttpFile

A library for parsing HTTP request and response files, similar to Microsoft's
VS Code .http files. It provides functionality to:

- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Extract variables from headers using token patterns like `[[VAR_NAME]]`
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

#### Header Variable Extraction

The `HeaderVariableExtractor` class provides functionality to extract variables from HTTP headers using token patterns:

```csharp
// Parse expected and actual headers
var expectedHeaders = new[]
{
    new ParsedHeader("Set-Cookie", "session=[[SESSION_TOKEN]]; Path=/", new Dictionary<string, string>()),
    new ParsedHeader("X-Token-Type", "[[TOKEN_TYPE]]", new Dictionary<string, string>())
};

var actualHeaders = new[]
{
    new ParsedHeader("Set-Cookie", "session=abc123; Path=/", new Dictionary<string, string>()),
    new ParsedHeader("X-Token-Type", "Bearer", new Dictionary<string, string>())
};

// Extract variables
var variables = HeaderVariableExtractor.ExtractVariables(expectedHeaders, actualHeaders);
// variables["SESSION_TOKEN"] = "abc123"
// variables["TOKEN_TYPE"] = "Bearer"
```

The extractor supports:
- Multiple tokens in the same header value
- Tokens in header parameters
- Case-insensitive header name matching
- Complex patterns with mixed literal text and tokens

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

var comparer = new JsonComparer();
bool isMatch = comparer.ExactMatch(expectedJson, actualJson, out var extractedValues, out var mismatches);
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

### PQSoft.ReqNRoll

Pre-built Reqnroll (BDD) step definitions for API testing. Write tests in plain Gherkin without boilerplate:

```gherkin
Scenario: Create a new job
  Given the following request
  """
  POST /api/job HTTP/1.1
  Content-Type: application/json
  
  {
    "JobType": "Upgrade"
  }
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 201 Created
  Content-Type: application/json
  
  {
    "jobId": [[JOBID]]
  }
  """
  
  Given the following request
  """
  GET /api/job/status/{{JOBID}} HTTP/1.1
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  
  {
    "jobId": "{{JOBID}}",
    "status": "Pending"
  }
  """
```

Features:
- Token extraction with `[[TOKEN]]` and substitution with `{{TOKEN}}`
- Header variable extraction from response headers
- Automatic HTTP request/response parsing
- JSON subset matching
- Header validation
- Variable type assertions
- Variable comparison between different sources

#### Header Variable Extraction

Extract variables from HTTP response headers and use them in subsequent requests:

```gherkin
Scenario: Extract session token and use in authenticated request
  Given the following request
  """
  POST /api/login HTTP/1.1
  Content-Type: application/json
  
  {
    "username": "testuser",
    "password": "testpass"
  }
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  Set-Cookie: session=[[SESSION_TOKEN]]; Path=/; HttpOnly
  Content-Type: application/json
  
  {
    "message": "Login successful"
  }
  """
  
  Given the following request
  """
  GET /api/profile HTTP/1.1
  Cookie: session={{SESSION_TOKEN}}
  """
  
  Then the API returns the following response
  """
  HTTP/1.1 200 OK
  Content-Type: application/json
  
  {
    "username": "testuser",
    "sessionId": "{{SESSION_TOKEN}}"
  }
  """
```

#### Variable Comparison

Compare variables extracted from different sources:

```gherkin
Then the API returns the following response
"""
HTTP/1.1 200 OK
X-Token-Type: [[HEADER_TOKEN_TYPE]]

{
  "tokenType": [[BODY_TOKEN_TYPE]]
}
"""

Then the variable 'HEADER_TOKEN_TYPE' equals the variable 'BODY_TOKEN_TYPE'
```

See [PQSoft.ReqNRoll README](src/PQSoft.ReqNRoll/README.md) for detailed examples.

## Installation

Install packages from NuGet:

```bash
# HTTP file parsing
dotnet add package PQSoft.HttpFile

# JSON comparison
dotnet add package PQSoft.JsonComparer

# JSON comparison with FluentAssertions
dotnet add package PQSoft.JsonComparer.AwesomeAssertions

# BDD/Reqnroll step definitions for API testing
dotnet add package PQSoft.ReqNRoll
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
dotnet pack src/PQSoft.ReqNRoll/PQSoft.ReqNRoll.csproj --configuration Release -p:Version=1.0.1

# Push to NuGet (in dependency order)
dotnet nuget push "src/PQSoft.HttpFile/bin/Release/PQSoft.HttpFile.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push "src/PQSoft.JsonComparer/bin/Release/PQSoft.JsonComparer.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push "src/PQSoft.JsonComparer.AwesomeAssertions/bin/Release/PQSoft.JsonComparer.AwesomeAssertions.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push "src/PQSoft.ReqNRoll/bin/Release/PQSoft.ReqNRoll.1.0.1.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

### Package Dependencies

- **PQSoft.HttpFile**: No external dependencies
- **PQSoft.JsonComparer**: Depends on System.Text.Json
- **PQSoft.JsonComparer.AwesomeAssertions**: Depends on PQSoft.JsonComparer and AwesomeAssertions
- **PQSoft.ReqNRoll**: Depends on PQSoft.HttpFile, PQSoft.JsonComparer, PQSoft.JsonComparer.AwesomeAssertions, and Reqnroll

Packages must be uploaded in dependency order to ensure successful publication.

## Documentation

Full documentation is available at: <https://edouardp.github.io/api-test-support/>

### Setting up GitHub Pages

To enable GitHub Pages for documentation:

```bash
# Enable Pages via GitHub CLI
gh api repos/edouardp/api-test-support/pages -X POST --input - <<< '{"source":{"branch":"main","path":"/docs"}}'
```

Documentation is automatically built and deployed from the `docs/` folder using Jekyll.

## Support Projects

- PQSoft.HttpFile.UnitTests
- PQSoft.JsonComparer.UnitTests
- PQSoft.JsonComparer.AwesomeAssertions.UnitTests

