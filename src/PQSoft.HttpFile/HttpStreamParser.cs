using System.Text;

namespace PQSoft.HttpFile;

/// <summary>
/// Represents a parsed HTTP request, including the HTTP method, URL, headers, and body.
/// </summary>
public record ParsedHttpRequest(HttpMethod Method, string Url, List<ParsedHeader> Headers, string Body)
{
    /// <summary>
    /// Converts the parsed HTTP request to an HttpRequestMessage.
    /// </summary>
    /// <returns>An HttpRequestMessage representing this parsed request.</returns>
    public HttpRequestMessage ToHttpRequestMessage()
    {
        var request = new HttpRequestMessage(Method, Url);

        // Content headers that belong on HttpContent, not HttpRequestMessage
        var contentHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Type", "Content-Length", "Content-Encoding", "Content-Language",
            "Content-Location", "Content-MD5", "Content-Range", "Expires", "Last-Modified"
        };

        // Find Content-Type header if present
        var contentTypeHeader = Headers.FirstOrDefault(h => 
            h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(Body))
        {
            if (contentTypeHeader != null)
            {
                var mediaType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentTypeHeader.Value);
                
                // Get encoding from charset parameter or default to UTF8
                var charset = contentTypeHeader.Parameters.GetValueOrDefault("charset", "utf-8");
                var encoding = GetEncodingFromCharset(charset);
                
                // Add all parameters
                foreach (var param in contentTypeHeader.Parameters)
                {
                    mediaType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue(param.Key, param.Value));
                }
                
                request.Content = new StringContent(Body, encoding, mediaType);
            }
            else
            {
                request.Content = new StringContent(Body, Encoding.UTF8, "text/plain");
            }
        }

        // Add headers to appropriate collection
        foreach (var header in Headers.Where(h => 
            !h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
        {
            if (contentHeaderNames.Contains(header.Name))
            {
                // Content header - only add if we have content
                if (request.Content != null)
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }
            else
            {
                // Request header
                request.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        return request;
    }

    private static Encoding GetEncodingFromCharset(string charset)
    {
        // Remove quotes if present
        charset = charset.Trim('"');
        
        return charset.ToLowerInvariant() switch
        {
            "utf-8" => Encoding.UTF8,
            "utf-16" => Encoding.Unicode,
            "utf-32" => Encoding.UTF32,
            "ascii" => Encoding.ASCII,
            "iso-8859-1" or "latin1" => Encoding.Latin1,
            _ => TryGetEncoding(charset)
        };
    }

    private static Encoding TryGetEncoding(string charset)
    {
        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException)
        {
            // Fallback to UTF-8 if encoding not supported
            return Encoding.UTF8;
        }
    }
};

/// <summary>
/// Provides functionality to parse an HTTP request from a stream.
/// </summary>
public static class HttpStreamParser
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
        var body = (await reader.ReadToEndAsync()).Trim();

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
        StringBuilder? currentHeaderBuilder = null;

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                ProcessPendingHeader(headers, currentHeaderBuilder?.ToString());
                return headers;
            }

            if (IsHeaderContinuation(line))
            {
                if (currentHeaderBuilder == null)
                {
                    throw new InvalidDataException($"Invalid HTTP header format: continuation line '{line.Trim()}' without preceding header.");
                }
                currentHeaderBuilder.Append(Space).Append(line.Trim());
            }
            else
            {
                ProcessPendingHeader(headers, currentHeaderBuilder?.ToString());
                currentHeaderBuilder = new StringBuilder(line);
            }
        }

        // Only process the final header if we reached end of stream without empty line
        ProcessPendingHeader(headers, currentHeaderBuilder?.ToString());
        return headers;
    }

    private static bool IsHeaderContinuation(string line)
    {
        return line.StartsWith(Space) || line.StartsWith(Tab);
    }

    private static void ProcessPendingHeader(List<ParsedHeader> headers, string? headerLine)
    {
        if (string.IsNullOrWhiteSpace(headerLine))
            return;

        var parsedHeader = HttpHeadersParser.ParseHeader(headerLine);
        headers.Add(parsedHeader);
    }
}
