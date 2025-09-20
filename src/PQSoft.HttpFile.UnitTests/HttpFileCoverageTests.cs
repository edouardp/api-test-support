using System.Text;

namespace PQSoft.HttpFile.UnitTests;

/// <summary>
/// Tests specifically designed to increase code coverage for HttpFile components
/// </summary>
public class HttpFileCoverageTests
{
    [Fact]
    public async Task HttpFileParser_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        const string nonExistentPath = "/path/that/does/not/exist.http";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await foreach (var _ in new HttpFileParser().ParseFileAsync(nonExistentPath))
            {
                // This should not execute
            }
        });
        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public async Task HttpStreamParser_NullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            HttpStreamParser.ParseAsync(null!));
    }

    [Fact]
    public async Task HttpStreamParser_NonReadableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var nonReadableStream = new MemoryStream();
        nonReadableStream.Close(); // Make it non-readable

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            HttpStreamParser.ParseAsync(nonReadableStream));
        Assert.Contains("must be readable", exception.Message);
    }

    [Fact]
    public async Task HttpStreamParser_EmptyStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            HttpStreamParser.ParseAsync(stream));
        Assert.Contains("missing request line", exception.Message);
    }

    [Fact]
    public async Task HttpStreamParser_InvalidRequestLine_ShouldThrowInvalidDataException()
    {
        // Arrange - Request line with only 2 parts instead of required 3
        const string invalidHttp = "GET https://example.com";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidHttp));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            HttpStreamParser.ParseAsync(stream));
        Assert.Contains("Invalid request line format", exception.Message);
    }

    [Fact]
    public async Task HttpStreamParser_ContinuationWithoutHeader_ShouldThrowInvalidDataException()
    {
        // Arrange
        const string invalidHttp = """
                                   GET https://example.com HTTP/1.1
                                    This is a continuation line without a preceding header
                                   """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidHttp));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            HttpStreamParser.ParseAsync(stream));
        Assert.Contains("continuation line", exception.Message);
        Assert.Contains("without preceding header", exception.Message);
    }

    [Fact]
    public void HttpHeadersParser_InvalidHeaderMissingSeparator_ShouldThrowArgumentException()
    {
        // Arrange
        const string invalidHeader = "InvalidHeaderWithoutColon";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            HttpHeadersParser.ParseHeader(invalidHeader));
        Assert.Contains("missing ':' separator", exception.Message);
    }

    [Fact]
    public void HttpHeadersParser_InvalidHeaderMissingKey_ShouldThrowArgumentException()
    {
        // Arrange
        const string invalidHeader = ": value-without-key";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            HttpHeadersParser.ParseHeader(invalidHeader));
        Assert.Contains("missing key", exception.Message);
    }

    [Fact]
    public async Task HttpResponseParser_EmptyStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            HttpResponseParser.ParseAsync(stream));
        Assert.Contains("missing status line", exception.Message);
    }

    [Fact]
    public async Task HttpResponseParser_InvalidStatusLine_ShouldThrowInvalidDataException()
    {
        // Arrange - Status line with only 2 parts instead of required 3
        const string invalidResponse = "HTTP/1.1 200";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidResponse));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            HttpResponseParser.ParseAsync(stream));
        Assert.Contains("Invalid HTTP status line format", exception.Message);
    }

    [Fact]
    public async Task HttpResponseParser_InvalidStatusCode_ShouldThrowInvalidDataException()
    {
        // Arrange
        var invalidResponse = "HTTP/1.1 INVALID_CODE OK";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidResponse));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            HttpResponseParser.ParseAsync(stream));
        Assert.Contains("Invalid status code", exception.Message);
    }

    [Fact]
    public void HttpLineSplitter_NullBaseStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HttpLineSplitter(null!));
    }

    [Fact]
    public async Task HttpFileParser_NullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in new HttpFileParser().ParseAsync(null!))
            {
                // This should not execute
            }
        });
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task HttpFileParser_NonReadableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var nonReadableStream = new MemoryStream();
        nonReadableStream.Close();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in new HttpFileParser().ParseAsync(nonReadableStream))
            {
                // This should not execute
            }
        });
        Assert.Contains("must be readable", exception.Message);
    }
}
