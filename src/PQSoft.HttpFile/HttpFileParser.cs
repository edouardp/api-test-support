namespace PQSoft.HttpFile;

using System.Runtime.CompilerServices;

/// <summary>
/// Provides functionality to parse HTTP requests from a file or stream.
/// The input can contain multiple requests separated by configurable separator lines (default: "###").
/// This parser uses a streaming approach to handle very large files without loading them into memory.
/// </summary>
public class HttpFileParser
{
    private readonly string requestSeparator;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpFileParser"/> class.
    /// </summary>
    /// <param name="separator">The line separator used to split requests (default: "###").</param>
    public HttpFileParser(string separator = "###")
    {
        requestSeparator = separator ?? throw new ArgumentNullException(nameof(separator));
    }

    /// <summary>
    /// Asynchronously parses HTTP requests from a given file path.
    /// This method streams the file and yields requests one at a time, making it suitable for very large files.
    /// </summary>
    /// <param name="filePath">The path to the file containing HTTP requests.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of <see cref="ParsedHttpRequest"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    public async IAsyncEnumerable<ParsedHttpRequest> ParseFileAsync(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' was not found.", filePath);
        }

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        await foreach (var request in ParseAsync(fileStream, cancellationToken).ConfigureAwait(false))
        {
            yield return request;
        }
    }

    /// <summary>
    /// Asynchronously parses HTTP requests from a given stream.
    /// The stream is expected to contain requests separated by the configured separator lines.
    /// This method uses a streaming approach and yields requests one at a time.
    /// </summary>
    /// <param name="httpStream">The input stream containing HTTP requests.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of <see cref="ParsedHttpRequest"/> objects.</returns>
    /// <exception cref="InvalidDataException">Thrown when the request format is invalid.</exception>
    public async IAsyncEnumerable<ParsedHttpRequest> ParseAsync(Stream httpStream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateInputStream(httpStream);

        var splitter = new HttpLineSplitter(httpStream, requestSeparator);

        await foreach (var segmentStream in splitter.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                var request = await HttpStreamParser.ParseAsync(segmentStream).ConfigureAwait(false);
                yield return request;
            }
            finally
            {
                await segmentStream.DisposeAsync();
            }
        }
    }

    private static void ValidateInputStream(Stream httpStream)
    {
        if (httpStream == null)
            throw new ArgumentNullException(nameof(httpStream), "HTTP stream cannot be null.");

        if (!httpStream.CanRead)
            throw new ArgumentException("HTTP stream must be readable.", nameof(httpStream));
    }
}
