using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;
using PQSoft.HttpFile;

namespace PQSoft.HttpFile.UnitTests;

public class ToHttpRequestMessageContentTypeTests
{
    [Fact]
    public async Task Should_Apply_JSON_ContentType_To_HttpRequestMessage()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Should_Apply_JSON_ContentType_With_Charset_To_HttpRequestMessage()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset=utf-8

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        httpRequest.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
    }

    [Fact]
    public async Task Should_Apply_XML_ContentType_To_HttpRequestMessage()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/xml

            <message>test</message>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");
    }

    [Fact]
    public async Task Should_Apply_PlainText_ContentType_To_HttpRequestMessage()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: text/plain

            Hello World
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Should_Apply_FormUrlEncoded_ContentType_To_HttpRequestMessage()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/x-www-form-urlencoded

            key1=value1&key2=value2
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
    }

    [Fact]
    public async Task Should_Apply_MultipartFormData_ContentType_With_Boundary_To_HttpRequestMessage()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; boundary=----WebKitFormBoundary

            ------WebKitFormBoundary
            Content-Disposition: form-data; name="file"

            file content
            ------WebKitFormBoundary--
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("multipart/form-data");
        httpRequest.Content.Headers.ContentType.Parameters.Should().Contain(p => 
            p.Name == "boundary" && p.Value == "----WebKitFormBoundary");
    }

    [Fact]
    public async Task Should_Default_To_TextPlain_When_No_ContentType_Specified()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1

            Some body content
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Should_Not_Add_ContentType_Header_To_Request_Headers()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        // Content-Type should NOT be in request headers (it's a content header)
        // We can't use Contains() because it throws, so check if trying to get it throws
        var hasContentTypeInRequestHeaders = httpRequest.Headers.Any(h => 
            h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
        hasContentTypeInRequestHeaders.Should().BeFalse();
        // It should be in content headers
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Handle_ContentType_With_Multiple_Parameters()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset=utf-8; boundary=something

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        httpRequest.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
        httpRequest.Content.Headers.ContentType.Parameters.Should().Contain(p => 
            p.Name == "boundary" && p.Value == "something");
    }

    [Fact]
    public async Task Should_Handle_ContentType_CaseInsensitive()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            content-type: application/json

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Should_Preserve_Other_Headers_When_ContentType_Present()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json
            Authorization: Bearer token123
            X-Custom-Header: custom-value

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        httpRequest.Headers.Authorization.Should().NotBeNull();
        httpRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequest.Headers.GetValues("X-Custom-Header").Should().Contain("custom-value");
    }

    [Fact]
    public async Task Should_Handle_ContentType_With_Quoted_Parameters()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset="utf-8"

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType.Should().NotBeNull();
        httpRequest.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        // CharSet property includes the quotes as parsed
        httpRequest.Content.Headers.ContentType.CharSet.Should().Be("\"utf-8\"");
    }

    [Fact]
    public async Task Should_Handle_Different_Charsets()
    {
        // Arrange
        var testCases = new[]
        {
            ("utf-8", "utf-8"),
            ("iso-8859-1", "iso-8859-1"),
            ("windows-1252", "windows-1252"),
            ("utf-16", "utf-16")
        };

        foreach (var (charset, expected) in testCases)
        {
            var rawHttp = $"POST https://api.example.com/data HTTP/1.1\nContent-Type: application/json; charset={charset}\n\n{{\"message\": \"test\"}}";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
            var request = await HttpStreamParser.ParseAsync(stream);

            // Act
            var httpRequest = request.ToHttpRequestMessage();

            // Assert
            httpRequest.Content!.Headers.ContentType!.CharSet.Should().Be(expected, 
                $"charset {charset} should be preserved");
        }
    }

    [Fact]
    public async Task Should_Handle_ContentLength_Header_Separately()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json
            Content-Length: 18

            {"message": "test"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        // Content-Length is automatically calculated by HttpClient, so we just verify it's not in request headers
        var hasContentLengthInRequestHeaders = httpRequest.Headers.Any(h => 
            h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase));
        hasContentLengthInRequestHeaders.Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_GET_Request_Without_Body_Or_ContentType()
    {
        // Arrange
        var request = new ParsedHttpRequest(
            HttpMethod.Get,
            "https://api.example.com/data",
            new List<ParsedHeader>
            {
                new ParsedHeader("Accept", "application/json", new Dictionary<string, string>())
            },
            string.Empty
        );

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().BeNull();
        httpRequest.Headers.Accept.Should().Contain(a => a.MediaType == "application/json");
    }

    [Fact]
    public async Task Should_Apply_ContentType_For_PUT_Request()
    {
        // Arrange
        var rawHttp = """
            PUT https://api.example.com/data/123 HTTP/1.1
            Content-Type: application/json

            {"message": "updated"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Put);
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Should_Apply_ContentType_For_PATCH_Request()
    {
        // Arrange
        var rawHttp = """
            PATCH https://api.example.com/data/123 HTTP/1.1
            Content-Type: application/json

            {"message": "patched"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Patch);
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
