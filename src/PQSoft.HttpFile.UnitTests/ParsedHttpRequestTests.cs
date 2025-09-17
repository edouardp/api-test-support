using AwesomeAssertions;
using System.Text;

namespace PQSoft.HttpFile.UnitTests;

public class ParsedHttpRequestTests
{
    [Fact]
    public void ToHttpRequestMessage_Should_Set_Method_And_Url()
    {
        // Arrange
        var request = new ParsedHttpRequest(HttpMethod.Post, "https://api.example.com/users", [], "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Add_All_Headers()
    {
        // Arrange
        var headers = new List<ParsedHeader>
        {
            new("Authorization", "Bearer token123", new Dictionary<string, string>()),
            new("X-Custom-Header", "custom-value", new Dictionary<string, string>()),
            new("User-Agent", "TestAgent/1.0", new Dictionary<string, string>())
        };
        var request = new ParsedHttpRequest(HttpMethod.Get, "https://api.example.com", headers, "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Headers.Should().Contain(h => h.Key == "Authorization" && h.Value.First() == "Bearer token123");
        httpRequest.Headers.Should().Contain(h => h.Key == "X-Custom-Header" && h.Value.First() == "custom-value");
        httpRequest.Headers.Should().Contain(h => h.Key == "User-Agent" && h.Value.First() == "TestAgent/1.0");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Set_Content_When_Body_Exists()
    {
        // Arrange
        var body = """{"name": "John", "age": 30}""";
        var request = new ParsedHttpRequest(HttpMethod.Post, "https://api.example.com", [], body);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be(body);
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Not_Set_Content_When_Body_Is_Empty()
    {
        // Arrange
        var request = new ParsedHttpRequest(HttpMethod.Get, "https://api.example.com", [], "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().BeNull();
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Not_Set_Content_When_Body_Is_Null()
    {
        // Arrange
        var request = new ParsedHttpRequest(HttpMethod.Get, "https://api.example.com", [], null!);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().BeNull();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public void ToHttpRequestMessage_Should_Handle_All_Http_Methods(string methodName)
    {
        // Arrange
        var method = new HttpMethod(methodName);
        var request = new ParsedHttpRequest(method, "https://api.example.com", [], "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(method);
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Handle_Complex_Url_With_Query_Parameters()
    {
        // Arrange
        var url = "https://api.example.com/users?page=1&limit=10&sort=name";
        var request = new ParsedHttpRequest(HttpMethod.Get, url, [], "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.RequestUri!.ToString().Should().Be(url);
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Handle_Headers_With_Special_Characters()
    {
        // Arrange
        var headers = new List<ParsedHeader>
        {
            new("X-Special-Chars", "value with spaces & symbols!", new Dictionary<string, string>()),
            new("X-Unicode", "café ñoño", new Dictionary<string, string>())
        };
        var request = new ParsedHttpRequest(HttpMethod.Post, "https://api.example.com", headers, "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Headers.Should().Contain(h => h.Key == "X-Special-Chars" && h.Value.First() == "value with spaces & symbols!");
        httpRequest.Headers.Should().Contain(h => h.Key == "X-Unicode" && h.Value.First() == "café ñoño");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Large_Body()
    {
        // Arrange
        var largeBody = new string('x', 10000);
        var request = new ParsedHttpRequest(HttpMethod.Post, "https://api.example.com", [], largeBody);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be(largeBody);
        content.Length.Should().Be(10000);
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Handle_Empty_Headers_List()
    {
        // Arrange
        var request = new ParsedHttpRequest(HttpMethod.Get, "https://api.example.com", [], "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Headers.Should().BeEmpty();
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Handle_Multiple_Headers_With_Same_Name()
    {
        // Arrange
        var headers = new List<ParsedHeader>
        {
            new("Accept", "application/json", new Dictionary<string, string>()),
            new("Accept", "text/plain", new Dictionary<string, string>())
        };
        var request = new ParsedHttpRequest(HttpMethod.Get, "https://api.example.com", headers, "");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        var acceptHeaders = httpRequest.Headers.Where(h => h.Key == "Accept").SelectMany(h => h.Value).ToList();
        acceptHeaders.Should().Contain("application/json");
        acceptHeaders.Should().Contain("text/plain");
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Create_New_Instance_Each_Time()
    {
        // Arrange
        var request = new ParsedHttpRequest(HttpMethod.Get, "https://api.example.com", [], "test");

        // Act
        var httpRequest1 = request.ToHttpRequestMessage();
        var httpRequest2 = request.ToHttpRequestMessage();

        // Assert
        httpRequest1.Should().NotBeSameAs(httpRequest2);
        httpRequest1.Method.Should().Be(httpRequest2.Method);
        httpRequest1.RequestUri.Should().Be(httpRequest2.RequestUri);
    }

    [Fact]
    public void ToHttpRequestMessage_Should_Handle_Content_Type_Header()
    {
        // Arrange
        var headers = new List<ParsedHeader>
        {
            new("Content-Type", "application/json", new Dictionary<string, string>())
        };
        var request = new ParsedHttpRequest(HttpMethod.Post, "https://api.example.com", headers, """{"test": "data"}""");

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Content.Should().NotBeNull();
        // Content-Type header should be present (StringContent sets its own default, but header is still added)
        var contentHeaders = httpRequest.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
        contentHeaders.Should().Contain(h => h.Key == "Content-Type");
    }

    // Integration tests using raw HTTP strings and Parse method
    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_GET_Request()
    {
        // Arrange
        var rawHttp = """
            GET https://api.example.com/users HTTP/1.1
            Host: api.example.com
            Authorization: Bearer token123
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/users");
        httpRequest.Headers.Should().Contain(h => h.Key == "Host" && h.Value.First() == "api.example.com");
        httpRequest.Headers.Should().Contain(h => h.Key == "Authorization" && h.Value.First() == "Bearer token123");
        httpRequest.Content.Should().BeNull();
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_POST_Request_With_JSON_Body()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/users HTTP/1.1
            Content-Type: application/json
            Authorization: Bearer token123

            {"name": "John Doe", "email": "john@example.com"}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/users");
        httpRequest.Headers.Should().Contain(h => h.Key == "Authorization" && h.Value.First() == "Bearer token123");
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be("""{"name": "John Doe", "email": "john@example.com"}""");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_PUT_Request_With_XML_Body()
    {
        // Arrange
        var rawHttp = """
            PUT https://api.example.com/users/123 HTTP/1.1
            Content-Type: application/xml
            Accept: application/xml

            <user><name>Jane Doe</name><email>jane@example.com</email></user>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Put);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/users/123");
        httpRequest.Headers.Should().Contain(h => h.Key == "Accept" && h.Value.First() == "application/xml");
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be("<user><name>Jane Doe</name><email>jane@example.com</email></user>");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_DELETE_Request()
    {
        // Arrange
        var rawHttp = """
            DELETE https://api.example.com/users/123 HTTP/1.1
            Authorization: Bearer token123
            X-Request-ID: abc-123-def
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Delete);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/users/123");
        httpRequest.Headers.Should().Contain(h => h.Key == "Authorization" && h.Value.First() == "Bearer token123");
        httpRequest.Headers.Should().Contain(h => h.Key == "X-Request-ID" && h.Value.First() == "abc-123-def");
        httpRequest.Content.Should().BeNull();
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Request_With_Query_Parameters()
    {
        // Arrange
        var rawHttp = """
            GET https://api.example.com/search?q=test&limit=10&offset=20 HTTP/1.1
            User-Agent: TestClient/1.0
            Accept: application/json
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/search?q=test&limit=10&offset=20");
        httpRequest.Headers.Should().Contain(h => h.Key == "User-Agent" && h.Value.First() == "TestClient/1.0");
        httpRequest.Headers.Should().Contain(h => h.Key == "Accept" && h.Value.First() == "application/json");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Request_With_Multiple_Headers_Same_Name()
    {
        // Arrange
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json
            Accept: text/plain
            X-Custom: value1
            X-Custom: value2
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        var acceptHeaders = httpRequest.Headers.Where(h => h.Key == "Accept").SelectMany(h => h.Value).ToList();
        acceptHeaders.Should().Contain("application/json");
        acceptHeaders.Should().Contain("text/plain");
        
        var customHeaders = httpRequest.Headers.Where(h => h.Key == "X-Custom").SelectMany(h => h.Value).ToList();
        customHeaders.Should().Contain("value1");
        customHeaders.Should().Contain("value2");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_PATCH_Request_With_Form_Data()
    {
        // Arrange
        var rawHttp = """
            PATCH https://api.example.com/profile HTTP/1.1
            Content-Type: application/x-www-form-urlencoded
            Authorization: Bearer token123

            name=John+Doe&email=john%40example.com&age=30
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Patch);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/profile");
        httpRequest.Headers.Should().Contain(h => h.Key == "Authorization" && h.Value.First() == "Bearer token123");
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be("name=John+Doe&email=john%40example.com&age=30");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Request_With_Special_Characters_In_Headers()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            X-Special-Chars: value with spaces & symbols!
            X-Unicode: café ñoño
            Content-Type: text/plain

            Hello, World! 🌍
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Headers.Should().Contain(h => h.Key == "X-Special-Chars" && h.Value.First() == "value with spaces & symbols!");
        httpRequest.Headers.Should().Contain(h => h.Key == "X-Unicode" && h.Value.First() == "café ñoño");
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be("Hello, World! 🌍");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Request_With_Large_JSON_Body()
    {
        // Arrange
        var largeJson = """{"data": [""" + string.Join(",", Enumerable.Range(1, 1000).Select(i => $""""{i}"""")) + """]}""";
        var rawHttp = $"""
            POST https://api.example.com/bulk HTTP/1.1
            Content-Type: application/json
            Content-Length: {Encoding.UTF8.GetByteCount(largeJson)}

            {largeJson}
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/bulk");
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be(largeJson);
        content.Length.Should().BeGreaterThan(3000);
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_OPTIONS_Request()
    {
        // Arrange
        var rawHttp = """
            OPTIONS https://api.example.com/users HTTP/1.1
            Origin: https://example.com
            Access-Control-Request-Method: POST
            Access-Control-Request-Headers: Content-Type, Authorization
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Options);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/users");
        httpRequest.Headers.Should().Contain(h => h.Key == "Origin" && h.Value.First() == "https://example.com");
        httpRequest.Headers.Should().Contain(h => h.Key == "Access-Control-Request-Method" && h.Value.First() == "POST");
        httpRequest.Headers.Should().Contain(h => h.Key == "Access-Control-Request-Headers" && h.Value.First() == "Content-Type, Authorization");
        httpRequest.Content.Should().BeNull();
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_HEAD_Request()
    {
        // Arrange
        var rawHttp = """
            HEAD https://api.example.com/status HTTP/1.1
            User-Agent: HealthCheck/1.0
            Accept: */*
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Head);
        httpRequest.RequestUri!.ToString().Should().Be("https://api.example.com/status");
        httpRequest.Headers.Should().Contain(h => h.Key == "User-Agent" && h.Value.First() == "HealthCheck/1.0");
        httpRequest.Headers.Should().Contain(h => h.Key == "Accept" && h.Value.First() == "*/*");
        httpRequest.Content.Should().BeNull();
    }

    // Content-Type and Body Format Tests
    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_JSON_Content_Type()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            Content-Type: application/json; charset=utf-8
            Accept: application/json

            {
              "id": 123,
              "name": "Test User",
              "active": true,
              "metadata": {
                "created": "2024-01-01T00:00:00Z",
                "tags": ["user", "test"]
              }
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("\"id\": 123");
        content.Should().Contain("\"name\": \"Test User\"");
        content.Should().Contain("\"active\": true");
        content.Should().Contain("\"tags\": [\"user\", \"test\"]");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_XML_Content_Type()
    {
        // Arrange
        var rawHttp = """
            PUT https://api.example.com/config HTTP/1.1
            Content-Type: application/xml; charset=utf-8
            Accept: application/xml

            <?xml version="1.0" encoding="UTF-8"?>
            <configuration>
              <setting name="timeout">30</setting>
              <setting name="retries">3</setting>
              <features>
                <feature enabled="true">logging</feature>
                <feature enabled="false">debug</feature>
              </features>
            </configuration>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Put);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        content.Should().Contain("<configuration>");
        content.Should().Contain("<setting name=\"timeout\">30</setting>");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Multipart_Form_Data()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

            ------WebKitFormBoundary7MA4YWxkTrZu0gW
            Content-Disposition: form-data; name="title"

            My Document
            ------WebKitFormBoundary7MA4YWxkTrZu0gW
            Content-Disposition: form-data; name="file"; filename="document.txt"
            Content-Type: text/plain

            This is the content of the file.
            ------WebKitFormBoundary7MA4YWxkTrZu0gW--
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("------WebKitFormBoundary7MA4YWxkTrZu0gW");
        content.Should().Contain("Content-Disposition: form-data; name=\"title\"");
        content.Should().Contain("My Document");
        content.Should().Contain("filename=\"document.txt\"");
        content.Should().Contain("This is the content of the file.");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Plain_Text_Content()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/notes HTTP/1.1
            Content-Type: text/plain; charset=utf-8

            This is a simple text note.
            It can contain multiple lines.
            
            And even empty lines in between.
            Special characters: àáâãäåæçèéêë
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("This is a simple text note.");
        content.Should().Contain("It can contain multiple lines.");
        content.Should().Contain("Special characters: àáâãäåæçèéêë");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_HTML_Content()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/content HTTP/1.1
            Content-Type: text/html; charset=utf-8

            <!DOCTYPE html>
            <html>
            <head>
                <title>Test Page</title>
            </head>
            <body>
                <h1>Hello World</h1>
                <p>This is a <strong>test</strong> HTML document.</p>
                <ul>
                    <li>Item 1</li>
                    <li>Item 2</li>
                </ul>
            </body>
            </html>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("<title>Test Page</title>");
        content.Should().Contain("<h1>Hello World</h1>");
        content.Should().Contain("<strong>test</strong>");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Binary_Content_Type()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/binary HTTP/1.1
            Content-Type: application/octet-stream
            Content-Length: 12

            Binary data here
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Be("Binary data here");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_CSV_Content()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/import HTTP/1.1
            Content-Type: text/csv; charset=utf-8

            Name,Age,Email
            John Doe,30,john@example.com
            Jane Smith,25,jane@example.com
            Bob Johnson,35,bob@example.com
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("Name,Age,Email");
        content.Should().Contain("John Doe,30,john@example.com");
        content.Should().Contain("Jane Smith,25,jane@example.com");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_JavaScript_Content()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/script HTTP/1.1
            Content-Type: application/javascript

            function processData(data) {
                console.log('Processing:', data);
                return data.map(item => ({
                    ...item,
                    processed: true,
                    timestamp: new Date().toISOString()
                }));
            }
            
            module.exports = { processData };
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("function processData(data)");
        content.Should().Contain("console.log('Processing:', data);");
        content.Should().Contain("module.exports = { processData };");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_YAML_Content()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/config HTTP/1.1
            Content-Type: application/x-yaml

            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: app-config
              namespace: default
            data:
              database_url: "postgresql://localhost:5432/myapp"
              redis_url: "redis://localhost:6379"
              log_level: "info"
              features:
                - authentication
                - logging
                - monitoring
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        var content = await httpRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("apiVersion: v1");
        content.Should().Contain("kind: ConfigMap");
        content.Should().Contain("database_url: \"postgresql://localhost:5432/myapp\"");
        content.Should().Contain("- authentication");
    }

    // Header Parameter Tests
    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Content_Type_With_Charset()
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
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        // Verify the parsed header has the parameter
        var contentTypeHeader = request.Headers.First(h => h.Name == "Content-Type");
        contentTypeHeader.Value.Should().Be("application/json");
        contentTypeHeader.Parameters.Should().ContainKey("charset").WhoseValue.Should().Be("utf-8");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Content_Type_With_Multiple_Parameters()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Type: multipart/form-data; boundary=----WebKit123; charset=utf-8

            ------WebKit123
            Content-Disposition: form-data; name="file"

            data
            ------WebKit123--
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        // Verify the parsed header has multiple parameters
        var contentTypeHeader = request.Headers.First(h => h.Name == "Content-Type");
        contentTypeHeader.Value.Should().Be("multipart/form-data");
        contentTypeHeader.Parameters.Should().ContainKey("boundary").WhoseValue.Should().Be("----WebKit123");
        contentTypeHeader.Parameters.Should().ContainKey("charset").WhoseValue.Should().Be("utf-8");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Accept_With_Quality_Values()
    {
        // Arrange
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json; q=0.9
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        
        // Verify the parsed header has the main value and parameters
        var acceptHeader = request.Headers.First(h => h.Name == "Accept");
        acceptHeader.Value.Should().Be("application/json");
        acceptHeader.Parameters.Should().ContainKey("q").WhoseValue.Should().Be("0.9");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Cache_Control_With_Parameters()
    {
        // Arrange
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Cache-Control: max-age=3600; must-revalidate=; private=
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        
        // Verify the parsed header has the main value and parameters
        var cacheHeader = request.Headers.First(h => h.Name == "Cache-Control");
        cacheHeader.Value.Should().Be("max-age=3600");
        cacheHeader.Parameters.Should().ContainKey("must-revalidate").WhoseValue.Should().Be("");
        cacheHeader.Parameters.Should().ContainKey("private").WhoseValue.Should().Be("");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Authorization_With_Parameters()
    {
        // Arrange
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Authorization: Digest username="user"; realm="api"; nonce="abc123"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        
        // Verify the parsed header has the main value and parameters
        var authHeader = request.Headers.First(h => h.Name == "Authorization");
        authHeader.Value.Should().Be("Digest username=\"user\"");
        authHeader.Parameters.Should().ContainKey("realm").WhoseValue.Should().Be("\"api\"");
        authHeader.Parameters.Should().ContainKey("nonce").WhoseValue.Should().Be("\"abc123\"");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Parsed_Content_Disposition_With_Parameters()
    {
        // Arrange
        var rawHttp = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Disposition: attachment; filename="document.pdf"; size=12345

            PDF content here
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        httpRequest.Content.Should().NotBeNull();
        
        // Verify the parsed header has the main value and parameters
        var dispositionHeader = request.Headers.First(h => h.Name == "Content-Disposition");
        dispositionHeader.Value.Should().Be("attachment");
        dispositionHeader.Parameters.Should().ContainKey("filename").WhoseValue.Should().Be("\"document.pdf\"");
        dispositionHeader.Parameters.Should().ContainKey("size").WhoseValue.Should().Be("12345");
    }

    // HTTP Spec Compliance Tests (these may fail due to current implementation limitations)
    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Accept_Header_With_Comma_Separated_Values()
    {
        // Arrange - RFC 9110: Accept header uses commas to separate multiple values
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json, text/plain, */*
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        
        // Current implementation treats this as one value, but HTTP spec says it should be multiple values
        var acceptHeader = request.Headers.First(h => h.Name == "Accept");
        // This test documents current behavior - ideally should parse multiple values
        acceptHeader.Value.Should().Be("application/json, text/plain, */*");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Accept_With_Quality_And_Multiple_Values()
    {
        // Arrange - RFC 9110: Accept with quality values and multiple media types
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Accept: application/json;q=0.9, text/plain;q=0.8, */*;q=0.1
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        
        // Current implementation incorrectly parses this due to semicolon splitting
        var acceptHeader = request.Headers.First(h => h.Name == "Accept");
        // This documents current behavior - takes last parameter value due to incorrect parsing
        acceptHeader.Value.Should().Be("application/json");
        acceptHeader.Parameters.Should().ContainKey("q").WhoseValue.Should().Be("0.1"); // Last value wins
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Cache_Control_Comma_Separated_Directives()
    {
        // Arrange - RFC 9111: Cache-Control uses commas to separate directives
        var rawHttp = """
            GET https://api.example.com/data HTTP/1.1
            Cache-Control: max-age=3600, must-revalidate, private
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Get);
        
        // Current implementation treats this as one value, but should parse multiple directives
        var cacheHeader = request.Headers.First(h => h.Name == "Cache-Control");
        cacheHeader.Value.Should().Be("max-age=3600, must-revalidate, private");
        // Parameters should be empty since commas separate directives, not semicolons
        cacheHeader.Parameters.Should().BeEmpty();
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Quoted_String_In_Header_Value()
    {
        // Arrange - RFC 9110: Quoted strings in header values
        var rawHttp = """
            POST https://api.example.com/upload HTTP/1.1
            Content-Disposition: attachment; filename="my document.pdf"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        
        // Current implementation preserves quotes but doesn't handle quoted-string semantics
        var dispositionHeader = request.Headers.First(h => h.Name == "Content-Disposition");
        dispositionHeader.Value.Should().Be("attachment");
        dispositionHeader.Parameters.Should().ContainKey("filename").WhoseValue.Should().Be("\"my document.pdf\"");
    }

    [Fact]
    public async Task ToHttpRequestMessage_Should_Handle_Header_With_Escaped_Quotes()
    {
        // Arrange - RFC 9110: Escaped characters in quoted strings
        var rawHttp = """
            POST https://api.example.com/data HTTP/1.1
            X-Custom-Header: value; param="quoted \"inner\" value"
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawHttp));
        var request = await HttpStreamParser.ParseAsync(stream);

        // Act
        var httpRequest = request.ToHttpRequestMessage();

        // Assert
        httpRequest.Method.Should().Be(HttpMethod.Post);
        
        // Current implementation doesn't handle escaped quotes properly
        var customHeader = request.Headers.First(h => h.Name == "X-Custom-Header");
        customHeader.Value.Should().Be("value");
        // This documents current behavior - escaped quotes not properly handled
        customHeader.Parameters.Should().ContainKey("param");
    }
}
