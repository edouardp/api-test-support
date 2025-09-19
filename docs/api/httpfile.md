# PQSoft.HttpFile

A library for parsing HTTP request and response files, similar to Microsoft's VS Code .http files.

## Features

- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Semantically compare parsed headers for testing purposes
- Convert parsed requests to `HttpRequestMessage` objects for integration with HttpClient

## Usage

### Parsing HTTP Requests

```csharp
var rawHttp = """
    POST https://api.example.com/users HTTP/1.1
    Content-Type: application/json
    Authorization: Bearer token123

    {"name": "John", "age": 30}
    """;

using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
var parsedRequest = await HttpStreamParser.ParseAsync(stream);
```

### Converting to HttpRequestMessage

```csharp
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
