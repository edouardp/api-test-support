namespace TestSupport.HttpFile;

/// <summary>
/// Represents a parsed HTTP request, including the HTTP method, URL, headers, and body.
/// </summary>
public record ParsedHttpRequest(HttpMethod Method, string Url, List<ParsedHeader> Headers, string Body);

/// <summary>
/// Provides functionality to parse an HTTP request from a stream.
/// </summary>
public class HttpFileParser
{
    private const int MinRequestLineParts = 3;
    private const char Space = ' ';
    private const char Tab = '\t';

    /// <summary>
    /// Asynchronously parses an HTTP request from a given stream.
    /// </summary>
    /// <param name="httpStream">The input stream containing the HTTP request.</param>
    /// <returns>A <see cref="ParsedHttpRequest"/> object containing the parsed request data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the request format is invalid.</exception>
    public static async Task<ParsedHttpRequest> ParseAsync(Stream httpStream)
    {
        ValidateInputStream(httpStream);
        
        using var reader = new StreamReader(httpStream);

        var (method, url) = await ParseRequestLineAsync(reader);
        var headers = await ParseHeadersAsync(reader);
        var body = await reader.ReadToEndAsync();

        return new ParsedHttpRequest(method, url, headers, body);
    }

    private static void ValidateInputStream(Stream httpStream)
    {
        if (httpStream == null)
            throw new ArgumentNullException(nameof(httpStream), "HTTP stream cannot be null.");
        
        if (!httpStream.CanRead)
            throw new ArgumentException("HTTP stream must be readable.", nameof(httpStream));
    }

    private static async Task<(HttpMethod Method, string Url)> ParseRequestLineAsync(StreamReader reader)
    {
        var requestLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            throw new InvalidDataException("Invalid HTTP file format: missing request line.");
        }

        var parts = requestLine.Split(Space, MinRequestLineParts);
        if (parts.Length < MinRequestLineParts)
        {
            throw new InvalidDataException($"Invalid request line format: '{requestLine}'. Expected: METHOD URL VERSION");
        }

        var (methodString, url, _) = (parts[0], parts[1], parts[2]);
        var method = new HttpMethod(methodString.ToUpperInvariant());
        
        return (method, url);
    }

    private static async Task<List<ParsedHeader>> ParseHeadersAsync(StreamReader reader)
    {
        var headers = new List<ParsedHeader>();
        string? currentHeaderLine = null;
        
        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                ProcessPendingHeader(headers, currentHeaderLine);
                return headers;
            }
            
            if (IsHeaderContinuation(line))
            {
                if (currentHeaderLine == null)
                {
                    throw new InvalidDataException($"Invalid HTTP header format: continuation line '{line.Trim()}' without preceding header.");
                }
                currentHeaderLine += Space + line.Trim();
            }
            else
            {
                ProcessPendingHeader(headers, currentHeaderLine);
                currentHeaderLine = line;
            }
        }
        
        // Only process the final header if we reached end of stream without empty line
        ProcessPendingHeader(headers, currentHeaderLine);
        return headers;
    }

    private static bool IsHeaderContinuation(string line)
    {
        return line.StartsWith(Space) || line.StartsWith(Tab);
    }

    private static void ProcessPendingHeader(List<ParsedHeader> headers, string? headerLine)
    {
        if (!string.IsNullOrWhiteSpace(headerLine))
        {
            var parsedHeader = HttpHeadersParser.ParseHeader(headerLine);
            headers.Add(parsedHeader);
        }
    }
}