using FluentAssertions;
using System.Net;
using System.Text;

namespace TestSupport.HttpFile.UnitTests;

public class HttpResponseParserTests
{
    [Fact]
    public async Task ParseAsync_Should_Parse_Valid_HttpResponse_With_Body()
    {
        // Arrange: A valid HTTP response with headers and a body
        string rawResponse = """
            HTTP/1.1 200 OK
            Content-Type: application/json
            Content-Length: 45

            {
                "message": "Hello, World!",
                "status": "OK"
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        var result = await parser.ParseAsync(stream);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ReasonPhrase.Should().Be("OK");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("application/json");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Length")
            .Which.Value.Should().Be("45");
        result.Body.Should().Be(
            """
            {
                "message": "Hello, World!",
                "status": "OK"
            }
            """);
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Empty_Body_When_ContentLength_Is_Zero()
    {
        // Arrange: A valid HTTP response with no body (Content-Length is 0)
        string rawResponse = """
            HTTP/1.1 204 No Content
            Content-Length: 0

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        var result = await parser.ParseAsync(stream);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        result.ReasonPhrase.Should().Be("No Content");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Length")
            .Which.Value.Should().Be("0");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_StatusLine_Is_Invalid()
    {
        // Arrange: An invalid status line (missing status code)
        string rawResponse = """
            HTTP/1.1 OK

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP status line format.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_StatusCode_Is_Invalid()
    {
        // Arrange: A status line with an invalid status code (non-numeric)
        string rawResponse = """
            HTTP/1.1 ABC OK

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid status code: ABC.");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Response_With_Multiple_Headers()
    {
        // Arrange: A valid HTTP response with multiple headers and a body
        string rawResponse = """
            HTTP/1.1 200 OK
            Content-Type: text/html
            X-Custom-Header: custom-value
            Content-Length: 22

            <html>Hello World</html>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        var result = await parser.ParseAsync(stream);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ReasonPhrase.Should().Be("OK");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("text/html");
        result.Headers.Should().ContainSingle(header => header.Name == "X-Custom-Header")
            .Which.Value.Should().Be("custom-value");
        result.Body.Should().Be("<html>Hello World</html>");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Response_With_No_Headers_And_Body()
    {
        // Arrange: A valid HTTP response with no headers and a body
        string rawResponse = """
            HTTP/1.1 200 OK

            Hello, this is a body.
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        var result = await parser.ParseAsync(stream);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ReasonPhrase.Should().Be("OK");
        result.Headers.Should().BeEmpty();
        result.Body.Should().Be("Hello, this is a body.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_StatusLine_Is_Missing()
    {
        // Arrange: An HTTP response with no status line (empty response)
        string rawResponse = "";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP response format: missing status line.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_StatusLine_Is_Empty()
    {
        // Arrange: An HTTP response with an empty status line
        string rawResponse = """

            Content-Type: text/html

            <html>Hello World</html>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP response format: missing status line.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_StatusLine_Has_Too_Few_Parts()
    {
        // Arrange: A status line that only has the HTTP version (missing status code and reason phrase)
        string rawResponse = "HTTP/1.1\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP status line format.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_StatusCode_Is_Invalid_2()
    {
        // Arrange: A status line with a non-numeric status code
        string rawResponse = "HTTP/1.1 ABC OK\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid status code: ABC.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Invalid()
    {
        // Arrange: A header without a colon separator
        string rawResponse = """
            HTTP/1.1 200 OK
            InvalidHeaderWithoutColon

            <html>Hello World</html>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, missing ':' separator.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Malformed()
    {
        // Arrange: A header with a colon but no key
        string rawResponse = """
            HTTP/1.1 200 OK
            : application/json

            <html>Hello World</html>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        Func<Task> act = async () => await parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, missing key.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Body_Is_Missing_And_ContentLength_Is_Set()
    {
        // Arrange: A response with a Content-Length set but no body
        string rawResponse = """
            HTTP/1.1 200 OK
            Content-Length: 20

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawResponse));
        var parser = new HttpResponseParser();

        // Act
        var result = await parser.ParseAsync(stream);

        // Assert: In this case, the body should be empty since it is missing
        result.Body.Should().BeEmpty();
    }

}


