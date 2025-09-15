using System.Text;

namespace PQSoft.HttpFile;

/// <summary>
/// Splits an HTTP file stream into individual request sections based on line-based separators.
/// Follows the .http file format where separators are lines that start with "###".
/// </summary>
public class HttpLineSplitter : IAsyncEnumerable<Stream>
{
    private readonly Stream baseStream;
    private readonly string separator;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpLineSplitter"/> class.
    /// </summary>
    /// <param name="baseStream">The stream to split.</param>
    /// <param name="separator">The line separator (default: "###").</param>
    public HttpLineSplitter(Stream baseStream, string separator = "###")
    {
        this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        this.separator = separator;
    }

    /// <summary>
    /// Returns an async enumerator that yields HTTP request sections as streams.
    /// </summary>
    public IAsyncEnumerator<Stream> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new HttpLineSplitterEnumerator(baseStream, separator, cancellationToken);
    }

    /// <summary>
    /// An async enumerator that splits the base stream into HTTP request sections.
    /// </summary>
    private class HttpLineSplitterEnumerator : IAsyncEnumerator<Stream>
    {
        private readonly StreamReader reader;
        private readonly string separator;
        private readonly CancellationToken cancellationToken;
        private bool disposed = false;

        public HttpLineSplitterEnumerator(Stream baseStream, string separator, CancellationToken cancellationToken)
        {
            reader = new StreamReader(baseStream, Encoding.UTF8, leaveOpen: true);
            this.separator = separator;
            this.cancellationToken = cancellationToken;
        }

        public Stream Current { get; private set; } = null!;
        // TODO: Using the null forgiving operator here is a big red flag that needs fixing

        /// <summary>
        /// Moves to the next HTTP request section in the stream.
        /// </summary>
        public async ValueTask<bool> MoveNextAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lines = new List<string>();

            // Read lines until we hit a separator or end of stream
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if this line is a separator
                if (IsLineSeparator(line))
                {
                    // If we have content, return it as a section
                    if (HasMeaningfulContent(lines))
                    {
                        Current = CreateStreamFromLines(lines);
                        return true;
                    }
                    // If no meaningful content yet, clear and continue reading (skip empty sections)
                    lines.Clear();
                    continue;
                }

                lines.Add(line);
            }

            // End of stream - return remaining content if any
            if (!HasMeaningfulContent(lines)) return false;
            Current = CreateStreamFromLines(lines);
            return true;

        }

        /// <summary>
        /// Determines if a collection of lines has meaningful content (not just empty lines or whitespace).
        /// </summary>
        private static bool HasMeaningfulContent(List<string> lines)
        {
            return lines.Any(line => !string.IsNullOrWhiteSpace(line));
        }

        /// <summary>
        /// Determines if a line is a separator line.
        /// A separator line either exactly matches the separator or starts with the separator.
        /// </summary>
        private bool IsLineSeparator(string line)
        {
            var trimmedLine = line.Trim();
            return trimmedLine == separator || trimmedLine.StartsWith(separator + " ");
        }

        /// <summary>
        /// Creates a memory stream from a collection of lines.
        /// </summary>
        private static Stream CreateStreamFromLines(IEnumerable<string> lines)
        {
            // Skip leading empty lines that could cause parsing issues
            var nonEmptyLines = lines.SkipWhile(string.IsNullOrWhiteSpace).ToList();
            if (nonEmptyLines.Count == 0)
            {
                return new MemoryStream([], writable: false);
            }

            var content = string.Join(Environment.NewLine, nonEmptyLines);
            content += Environment.NewLine; // Add trailing newline
            var bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes, writable: false);
        }

        /// <summary>
        /// Disposes of managed resources.
        /// </summary>
        public ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                Current?.Dispose();
                // The conditional access is required because of the null forgiving "Current = null!" creation
                reader.Dispose();
                disposed = true;
            }
            return ValueTask.CompletedTask;
        }
    }
}
