using System.Net;

namespace TestSupport.HttpFile;

/// <summary>
/// Represents a parsed HTTP response with status code, reason phrase, headers, and body.
/// </summary>
public record ParsedHttpResponse(HttpStatusCode StatusCode, string ReasonPhrase, List<ParsedHeader> Headers, string Body);

/// <summary>
/// Parses an HTTP response from a given stream into a structured format.
/// </summary>
public class HttpResponseParser
{
    /// <summary>
    /// Asynchronously parses an HTTP response stream into a ParsedHttpResponse object.
    /// </summary>
    /// <param name="httpStream">The stream containing the HTTP response.</param>
    /// <returns>A ParsedHttpResponse object containing the status code, reason phrase, headers, and body.</returns>
    /// <exception cref="InvalidDataException">Thrown when the HTTP response format is invalid.</exception>
    public async Task<ParsedHttpResponse> ParseAsync(Stream httpStream)
    {
        using var reader = new StreamReader(httpStream);

        // Step 1: Parse the status line (e.g., "HTTP/1.1 200 OK")
        string? statusLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(statusLine))
        {
            throw new InvalidDataException("Invalid HTTP response format: missing status line.");
        }

        // Status line typically consists of 3 parts: HTTP version, status code, and reason phrase
        var statusLineParts = statusLine.Split(' ', 3);
        if (statusLineParts.Length < 3)
        {
            throw new InvalidDataException("Invalid HTTP status line format.");
        }

        // Convert status code to integer and cast to HttpStatusCode enum
        if (!int.TryParse(statusLineParts[1], out int statusCodeInt))
        {
            throw new InvalidDataException($"Invalid status code: {statusLineParts[1]}.");
        }
        var statusCode = (HttpStatusCode)statusCodeInt;

        // Extract the reason phrase from the status line
        string reasonPhrase = statusLineParts[2];

        // Step 2: Parse headers
        var headers = new List<ParsedHeader>();
        string? line;
        while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
        {
            // Parse individual headers using a helper parser
            var parsedHeader = HttpHeadersParser.ParseHeader(line);
            headers.Add(parsedHeader);
        }

        // Step 3: Read the body (if any)
        // Remaining content in the stream is treated as the body
        string body = await reader.ReadToEndAsync();

        // Return the parsed HTTP response as a structured record
        return new ParsedHttpResponse(statusCode, reasonPhrase, headers, body);
    }
}