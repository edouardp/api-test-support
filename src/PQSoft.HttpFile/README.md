# PQSoft.HttpFile

A library for parsing HTTP request and response files, similar to Microsoft's VS Code .http files.

## Features

- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Semantically compare parsed headers for testing purposes

This package is particularly useful for API testing scenarios where you need to parse and validate HTTP traffic.

## Installation

```bash
dotnet add package PQSoft.HttpFile
```

## Usage

```csharp
// Parse HTTP request
var request = HttpRequestParser.Parse(httpRequestString);

// Parse HTTP response
var response = HttpResponseParser.Parse(httpResponseString);

// Access parsed data
Console.WriteLine($"Method: {request.Method}");
Console.WriteLine($"URL: {request.Url}");
Console.WriteLine($"Status: {response.StatusCode}");
```
