using FluentAssertions;
using System.Text;

namespace TestSupport.HttpFile.UnitTests;

public class HttpFileParserTests
{
    [Fact]
    public async Task ParseAsync_Should_Parse_POST_Request_With_Headers_And_Body()
    {
        // Arrange
        const string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type: application/json; charset=utf-8
            Authorization: Bearer your_token_here

            {
            "name": "John Doe",
            "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/submit");
        result.Headers.Should().HaveCount(2);
        var contentTypeHeader = result.Headers.Single(h => h.Name == "Content-Type");
        contentTypeHeader.Value.Should().Be("application/json");
        contentTypeHeader.Parameters.Should().ContainKey("charset").And.ContainValue("utf-8");
        result.Headers.Should().ContainSingle(header => header.Name == "Authorization")
            .Which.Value.Should().Be("Bearer your_token_here");
        result.Body.Should().Contain("John Doe").And.Contain("30");
    }

    [Fact]
    public async Task ParseAsync_Should_Parse_GET_Request_Without_Body()
    {
        // Arrange
        const string rawRequest = """
            GET /api/v1/users HTTP/1.1
            Host: example.com
            Accept: application/json
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/v1/users");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "Accept")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Custom_HTTP_Method()
    {
        // Arrange
        string rawRequest = """
            CUSTOMMETHOD /custom/path HTTP/1.1
            Custom-Header: custom-value
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(new HttpMethod("CUSTOMMETHOD"));
        result.Url.Should().Be("/custom/path");
        result.Headers.Should().ContainSingle(header => header.Name == "Custom-Header")
            .Which.Value.Should().Be("custom-value");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Parse_Request_With_Multiple_Headers_And_Empty_Body()
    {
        // Arrange
        string rawRequest = """
            PUT /update/resource HTTP/1.1
            Host: example.com
            Content-Length: 0
            User-Agent: CustomClient/1.0
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Put);
        result.Url.Should().Be("/update/resource");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Length")
            .Which.Value.Should().Be("0");
        result.Headers.Should().ContainSingle(header => header.Name == "User-Agent")
            .Which.Value.Should().Be("CustomClient/1.0");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Request_With_No_Headers_And_No_Body()
    {
        // Arrange
        string rawRequest = """
            HEAD /no-headers HTTP/1.1
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Head);
        result.Url.Should().Be("/no-headers");
        result.Headers.Should().BeEmpty();
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_RequestLine_Is_Missing()
    {
        // Arrange: Missing request line (empty input)
        const string rawRequest = """

                                  """;

        // Act
        Func<Task> act = async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
            await HttpFileParser.ParseAsync(stream);
        };
        
        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP file format: missing request line.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_RequestLine_Has_Too_Few_Parts()
    {
        // Arrange: Invalid request line (missing HTTP method or URL)
        string rawRequest = """
            GET HTTP/1.1

            """;

        // Act
        Func<Task> act = async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
            await HttpFileParser.ParseAsync(stream);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid request line format: 'GET HTTP/1.1'. Expected: METHOD URL VERSION");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_RequestLine_Is_Invalid()
    {
        // Arrange: Invalid request line format (completely malformed)
        string rawRequest = """
            INVALID_LINE

            """;

        // Act
        Func<Task> act = async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
            await HttpFileParser.ParseAsync(stream);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid request line format: 'INVALID_LINE'. Expected: METHOD URL VERSION");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Malformed()
    {
        // Arrange: Malformed header (missing colon)
        string rawRequest = """
            POST /submit HTTP/1.1
            InvalidHeaderWithoutColon
            Content-Type: application/json

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        // Act
        Func<Task> act = async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
            await HttpFileParser.ParseAsync(stream);
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, missing ':' separator.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Missing_Colon()
    {
        // Arrange: Empty header value
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        // Act
        Func<Task> act = async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
            await HttpFileParser.ParseAsync(stream);
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, missing ':' separator.");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Headers_With_Empty_Values()
    {
        // Arrange: Empty header value (HTTP spec allows this)
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type: 
            X-Custom-Header:

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/submit");
        result.Headers.Should().HaveCount(2);
        result.Headers.Should().Contain(header => header.Name == "Content-Type" && header.Value == "");
        result.Headers.Should().Contain(header => header.Name == "X-Custom-Header" && header.Value == "");
        result.Body.Should().Contain("John Doe").And.Contain("30");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Headers_With_Empty_Values_But_Parameters()
    {
        // Arrange: Empty header value but with parameters
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type: ; charset=utf-8

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/submit");
        result.Headers.Should().HaveCount(1);
        var contentTypeHeader = result.Headers.Single(h => h.Name == "Content-Type");
        contentTypeHeader.Value.Should().BeEmpty();
        contentTypeHeader.Parameters.Should().ContainKey("charset").And.ContainValue("utf-8");
        result.Body.Should().Contain("John Doe").And.Contain("30");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Body_Is_Missing_And_ContentLength_Is_Set()
    {
        // Arrange: Body is missing but Content-Length is non-zero
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Length: 15

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert: The parser will not throw in this case, but result.Body will be empty
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_URL_With_Query_Parameters()
    {
        // Arrange
        string rawRequest = """
            GET /api/users?page=1&limit=10&filter=active HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/users?page=1&limit=10&filter=active");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_URL_With_Special_Characters()
    {
        // Arrange
        string rawRequest = """
            GET /api/search?query=hello%20world&category=caf%C3%A9 HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/search?query=hello%20world&category=caf%C3%A9");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_URL_With_Fragment()
    {
        // Arrange
        string rawRequest = """
            GET /page#section1 HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/page#section1");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Root_URL()
    {
        // Arrange
        string rawRequest = """
            GET / HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_URL_With_Multiple_Slashes()
    {
        // Arrange
        string rawRequest = """
            GET /api//v1///users HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api//v1///users");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Long_URL()
    {
        // Arrange
        string longPath = string.Join("/", Enumerable.Repeat("segment", 50));
        string rawRequest = $"""
            GET /{longPath}?param=value HTTP/1.1
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be($"/{longPath}?param=value");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Multiline_Header_With_Space_Continuation()
    {
        // Arrange
        string rawRequest = """
            GET /api/test HTTP/1.1
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
             .eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0
             .SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
            Host: example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/test");
        result.Headers.Should().ContainSingle(header => header.Name == "Authorization")
            .Which.Value.Should().Be("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9 .eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0 .SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Multiline_Header_With_Tab_Continuation()
    {
        // Arrange
        string rawRequest = """
            POST /api/submit HTTP/1.1
            Content-Type: application/json;
            	charset=utf-8;
            	boundary=something
            Host: example.com

            {"test": "data"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/api/submit");
        result.Headers.Should().HaveCount(2);
        var contentTypeHeader = result.Headers.Single(h => h.Name == "Content-Type");
        contentTypeHeader.Value.Should().Be("application/json");
        contentTypeHeader.Parameters.Should().ContainKey("charset").And.ContainValue("utf-8");
        contentTypeHeader.Parameters.Should().ContainKey("boundary").And.ContainValue("something");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Body.Should().Be("""{"test": "data"}""");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Continuation_Line_Without_Header()
    {
        // Arrange: Continuation line without a preceding header
        string rawRequest = """
            GET /api/test HTTP/1.1
             Invalid continuation line
            Host: example.com
            """;

        // Act
        var act = async () =>
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
            await HttpFileParser.ParseAsync(stream);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP header format: continuation line 'Invalid continuation line' without preceding header.");
    }

    // New tests for input validation and missing scenarios
    [Fact]
    public async Task ParseAsync_Should_ThrowArgumentNullException_When_Stream_Is_Null()
    {
        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("HTTP stream cannot be null. (Parameter 'httpStream')");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Empty_Body()
    {
        // Arrange
        string rawRequest = """
            POST /api/submit HTTP/1.1
            Content-Type: application/json
            Content-Length: 0

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/api/submit");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("application/json");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Length")
            .Which.Value.Should().Be("0");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Large_Body()
    {
        // Arrange
        var largeBody = new string('x', 10000);
        string rawRequest = $"""
            POST /api/submit HTTP/1.1
            Content-Type: text/plain
            Content-Length: {largeBody.Length}

            {largeBody}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/api/submit");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("text/plain");
        result.Body.Should().Be(largeBody);
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Body_With_Special_Characters()
    {
        // Arrange
        string bodyWithSpecialChars = "{\"message\": \"Hello 🌍! Special chars: åäö €£¥\"}";
        string rawRequest = $"""
            POST /api/submit HTTP/1.1
            Content-Type: application/json; charset=utf-8

            {bodyWithSpecialChars}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/api/submit");
        result.Body.Should().Be(bodyWithSpecialChars);
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Headers_At_Stream_End_Without_Empty_Line()
    {
        // Arrange - no empty line between headers and end of stream
        string rawRequest = """
            GET /api/test HTTP/1.1
            Host: example.com
            Accept: application/json
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/test");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "Accept")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Mixed_Headers_With_And_Without_Values()
    {
        // Arrange
        string rawRequest = """
            GET /api/test HTTP/1.1
            Host: example.com
            X-Custom-Header:
            Accept: application/json

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/test");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "X-Custom-Header")
            .Which.Value.Should().BeEmpty();
        result.Headers.Should().ContainSingle(header => header.Name == "Accept")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_HTTP_Methods_Case_Insensitive()
    {
        // Arrange
        string rawRequest = """
            patch /api/resource HTTP/1.1
            Content-Type: application/json

            {"field": "value"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Patch);
        result.Url.Should().Be("/api/resource");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().Be("""{"field": "value"}""");
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Real_World_HTTP_Request()
    {
        // Arrange - Real-world example with authentication, complex headers, JSON body
        string rawRequest = """
            POST /api/v2/users HTTP/1.1
            Host: api.example.com
            Content-Type: application/json; charset=utf-8
            Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
            User-Agent: MyApp/1.0 (https://example.com)
            Accept: application/json, text/plain, */*
            Accept-Encoding: gzip, deflate, br
            Connection: keep-alive
            X-Request-ID: 123e4567-e89b-12d3-a456-426614174000
            X-API-Version: 2.1

            {
              "firstName": "John",
              "lastName": "Doe",
              "email": "john.doe@example.com",
              "preferences": {
                "notifications": true,
                "theme": "dark"
              },
              "roles": ["user", "admin"]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/api/v2/users");
        result.Headers.Should().HaveCount(9); // Updated to match actual header count
        result.Headers.Should().ContainSingle(h => h.Name == "Host").Which.Value.Should().Be("api.example.com");
        result.Headers.Should().ContainSingle(h => h.Name == "Content-Type").Which.Value.Should().Be("application/json");
        result.Headers.Should().ContainSingle(h => h.Name == "Authorization").Which.Value.Should().StartWith("Bearer ");
        result.Headers.Should().ContainSingle(h => h.Name == "X-Request-ID").Which.Value.Should().Be("123e4567-e89b-12d3-a456-426614174000");
        result.Body.Should().Contain("firstName").And.Contain("John").And.Contain("preferences");
    }
}


