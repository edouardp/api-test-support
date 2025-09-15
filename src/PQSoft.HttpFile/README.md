# PQSoft.HttpFile

A .NET library for parsing HTTP request and response files, similar to Microsoft's VS Code .http files. This package provides functionality to parse, validate, and work with HTTP traffic data programmatically.

## Features

- **HTTP Request Parsing**: Parse HTTP requests with method, URL, headers, and body
- **HTTP Response Parsing**: Parse HTTP responses with status code, headers, and body  
- **Header Management**: Handle HTTP headers with parameters and complex values
- **Semantic Comparison**: Compare parsed headers for testing and validation purposes
- **VS Code .http File Compatibility**: Works with the same format as VS Code REST Client extension

## Installation

```bash
dotnet add package PQSoft.HttpFile
```

## Usage

### Parsing HTTP Requests

```csharp
using PQSoft.HttpFile;

string httpRequest = @"
POST https://api.example.com/users HTTP/1.1
Content-Type: application/json
Authorization: Bearer token123

{
    ""name"": ""John Doe"",
    ""email"": ""john@example.com""
}";

var parsedRequest = HttpRequestParser.Parse(httpRequest);

Console.WriteLine($"Method: {parsedRequest.Method}");
Console.WriteLine($"URL: {parsedRequest.Url}");
Console.WriteLine($"Content-Type: {parsedRequest.Headers["Content-Type"]}");
Console.WriteLine($"Body: {parsedRequest.Body}");
```

### Parsing HTTP Responses

```csharp
using PQSoft.HttpFile;

string httpResponse = @"
HTTP/1.1 201 Created
Content-Type: application/json
Location: https://api.example.com/users/123

{
    ""id"": 123,
    ""name"": ""John Doe"",
    ""email"": ""john@example.com"",
    ""createdAt"": ""2024-01-01T10:00:00Z""
}";

var parsedResponse = HttpResponseParser.Parse(httpResponse);

Console.WriteLine($"Status Code: {parsedResponse.StatusCode}");
Console.WriteLine($"Status Text: {parsedResponse.StatusText}");
Console.WriteLine($"Location: {parsedResponse.Headers["Location"]}");
Console.WriteLine($"Body: {parsedResponse.Body}");
```

### Working with Headers

```csharp
using PQSoft.HttpFile;

// Parse complex headers with parameters
string contentTypeHeader = "application/json; charset=utf-8; boundary=something";
var header = HttpHeader.Parse("Content-Type", contentTypeHeader);

Console.WriteLine($"Main Value: {header.Value}"); // application/json
Console.WriteLine($"Charset: {header.Parameters["charset"]}"); // utf-8
Console.WriteLine($"Boundary: {header.Parameters["boundary"]}"); // something

// Semantic header comparison for testing
var header1 = HttpHeader.Parse("Content-Type", "application/json; charset=utf-8");
var header2 = HttpHeader.Parse("Content-Type", "application/json;charset=utf-8");

bool areEqual = header1.SemanticallyEquals(header2); // true
```

### API Testing Scenarios

This library is particularly useful for API testing where you need to:

```csharp
// Validate API responses match expected format
var expectedResponse = HttpResponseParser.Parse(expectedHttpResponse);
var actualResponse = HttpResponseParser.Parse(actualHttpResponse);

// Compare status codes
Assert.Equal(expectedResponse.StatusCode, actualResponse.StatusCode);

// Compare headers semantically (ignoring whitespace differences)
foreach (var expectedHeader in expectedResponse.Headers)
{
    var actualHeader = actualResponse.Headers[expectedHeader.Key];
    Assert.True(expectedHeader.Value.SemanticallyEquals(actualHeader));
}

// Validate response body
Assert.Equal(expectedResponse.Body, actualResponse.Body);
```

## API Reference

### HttpRequestParser

- `Parse(string httpRequest)`: Parses an HTTP request string into an `HttpRequest` object

### HttpResponseParser  

- `Parse(string httpResponse)`: Parses an HTTP response string into an `HttpResponse` object

### HttpRequest

Properties:
- `Method`: HTTP method (GET, POST, etc.)
- `Url`: Request URL
- `Headers`: Dictionary of HTTP headers
- `Body`: Request body content

### HttpResponse

Properties:
- `StatusCode`: HTTP status code (200, 404, etc.)
- `StatusText`: HTTP status text (OK, Not Found, etc.)
- `Headers`: Dictionary of HTTP headers  
- `Body`: Response body content

### HttpHeader

Properties:
- `Value`: Main header value
- `Parameters`: Dictionary of header parameters

Methods:
- `SemanticallyEquals(HttpHeader other)`: Compares headers semantically

## Contributing

Contributions are welcome! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
