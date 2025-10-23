# PQSoft.HttpFile

A library for parsing HTTP request and response files, similar to Microsoft's VS
Code .http files.

## Features

- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Semantically compare parsed headers for testing purposes

This package is particularly useful for API testing scenarios where you need to
parse and validate HTTP traffic.

## Installation

```bash
dotnet add package PQSoft.HttpFile
```

## Usage

### Basic Request Parsing

```csharp
string httpRequest = @"POST /api/users HTTP/1.1
Host: example.com
Content-Type: application/json
Authorization: Bearer token123

{""name"": ""John"", ""email"": ""john@example.com""}";

var request = HttpRequestParser.Parse(httpRequest);
Console.WriteLine($"Method: {request.Method}");        // POST
Console.WriteLine($"URL: {request.Url}");              // /api/users
Console.WriteLine($"Host: {request.Headers["Host"]}"); // example.com
Console.WriteLine($"Body: {request.Body}");            // JSON content
```

### Basic Response Parsing

```csharp
string httpResponse = @"HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/users/123

{""id"": 123, ""name"": ""John"", ""email"": ""john@example.com""}";

var response = HttpResponseParser.Parse(httpResponse);
Console.WriteLine($"Status: {response.StatusCode}");           // 201
Console.WriteLine($"Reason: {response.ReasonPhrase}");         // Created
Console.WriteLine($"Location: {response.Headers["Location"]}"); // /api/users/123
```

### Header Comparison for Testing

```csharp
// Compare headers semantically (useful for testing)
var expected = new Dictionary<string, string> { {"Content-Type", "application/json"} };
var actual = response.Headers;

bool headersMatch = HttpHeaderComparer.Compare(expected, actual);
```

### Working with Complex Headers

```csharp
string requestWithComplexHeaders = @"GET /api/data HTTP/1.1
Host: api.example.com
Accept: application/json, text/plain; q=0.9
Cache-Control: no-cache, max-age=0
Cookie: session=abc123; theme=dark";

var request = HttpRequestParser.Parse(requestWithComplexHeaders);

// Access header parameters
var acceptHeader = request.Headers["Accept"];
var cookies = request.Headers["Cookie"];
```
